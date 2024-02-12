using DotJEM.Json.Index2.Management.Snapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management.Info;
using DotJEM.Json.Index2.Management.Tracking;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using DotJEM.Json.Index2.Snapshots;
using DotJEM.Json.Index2;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Web.UI.WebControls;

namespace DotJEM.Web.Host.Providers.Data.Index.Snapshots;

public class WebHostJsonIndexSnapshotManager : IJsonIndexSnapshotManager
{
    private readonly IJsonIndex index;
    private readonly ISnapshotStrategy strategy;
    private readonly IWebTaskScheduler scheduler;
    private readonly IInfoStream<JsonIndexSnapshotManager> infoStream = new InfoStream<JsonIndexSnapshotManager>();
    private readonly ISchemaCollection schemas;

    private readonly string schedule;

    public IInfoStream InfoStream => infoStream;

    public WebHostJsonIndexSnapshotManager(IJsonIndex index, ISnapshotStrategy snapshotStrategy, IWebTaskScheduler scheduler, ISchemaCollection schemas, string schedule)
    {
        this.index = index;

        this.scheduler = scheduler;
        this.schedule = schedule;
        this.schemas = schemas;
        this.strategy = snapshotStrategy;
        this.strategy.InfoStream.Subscribe(infoStream);
    }

    public async Task RunAsync(IIngestProgressTracker tracker, bool restoredFromSnapshot)
    {
        await tracker.WhenState(IngestInitializationState.Initialized).ConfigureAwait(false);
        if (!restoredFromSnapshot)
        {
            infoStream.WriteInfo("Taking snapshot after initialization.");
            await TakeSnapshotAsync(tracker.IngestState).ConfigureAwait(false);
        }
        scheduler.Schedule(nameof(JsonIndexSnapshotManager), _ => this.TakeSnapshotAsync(tracker.IngestState), schedule);
    }

    public async Task<bool> TakeSnapshotAsync(StorageIngestState state)
    {
        try
        {
            JObject json = JObject.FromObject(state);
            ISnapshotStorage target = strategy.Storage;

            index.Commit();
            ISnapshot snapshot = await index.TakeSnapshotAsync(target).ConfigureAwait(false);
            using (ISnapshotWriter writer = snapshot.OpenWriter())
            {
                using (JsonTextWriter wr = new(new StreamWriter(writer.OpenStream("manifest.json"))))
                {
                    await json.WriteToAsync(wr).ConfigureAwait(false);
                    await wr.FlushAsync().ConfigureAwait(false);
                }

                foreach (JSchema jSchema in schemas)
                {
                    using JsonTextWriter wr = new(new StreamWriter(writer.OpenStream($"schemas/{jSchema.ContentType}.json")));
                    await jSchema.Serialize("").WriteToAsync(wr).ConfigureAwait(false);
                    await wr.FlushAsync().ConfigureAwait(false);
                }
            }

            infoStream.WriteSnapshotCreatedEvent(snapshot, "Snapshot created.");
            return true;
        }
        catch (Exception exception)
        {
            infoStream.WriteError("Failed to take snapshot.", exception);
            return false;
        }
        finally
        {
            strategy.CleanOldSnapshots();
        }
    }

    public async Task<RestoreSnapshotResult> RestoreSnapshotAsync()
    {
        try
        {
            ISnapshotStorage source = strategy.Storage;
            if (source == null)
            {
                infoStream.WriteInfo($"No snapshots found to restore");
                return new RestoreSnapshotResult(false, default);
            }

            int count = 0;
            foreach (ISnapshot snapshot in source.LoadSnapshots())
            {
                count++;
                try
                {
                    if (snapshot.Verify() && await index.RestoreSnapshotAsync(snapshot).ConfigureAwait(false))
                    {
                        using ISnapshotReader reader = snapshot.OpenReader();
                        using Stream manifestStream = reader.OpenStream("manifest.json");
                        JObject manifest = await JObject.LoadAsync(new JsonTextReader(new StreamReader(manifestStream)))
                            .ConfigureAwait(false);
                        if (manifest["Areas"] is not JArray areas) continue;
                        foreach (string schemaPath in reader.FileNames.Where(name => name.StartsWith("schemas/")))
                            await LoadSchema(schemaPath, reader, manifestStream);
                        return new RestoreSnapshotResult(true, new StorageIngestState(areas.ToObject<StorageAreaIngestState[]>()));
                    }

                    snapshot.Verify();
                }
                catch (Exception ex)
                {
                    infoStream.WriteError($"Failed to restore snapshot {snapshot}.", ex);
                    snapshot.Verify();
                }
            }
            infoStream.WriteInfo($"No snapshots restored. {count} was found to be corrupt and was deleted.");
            return new RestoreSnapshotResult(false, new StorageIngestState());
        }
        catch (Exception ex)
        {
            infoStream.WriteError("Failed to restore snapshot.", ex);
            return new RestoreSnapshotResult(false, default);
        }
    }

    private async Task LoadSchema(string schemaPath, ISnapshotReader reader, Stream manifestStream)
    {
        try
        {
            string schemaName = schemaPath.Split('/').Last();
            using Stream schemaStream = reader.OpenStream(schemaPath);
            JObject schema = await JObject.LoadAsync(new JsonTextReader(new StreamReader(manifestStream)))
                .ConfigureAwait(false);
            schemas.AddOrUpdate(schemaName, schema.ToObject<JSchema>());
        }
        catch (Exception ex)
        {
            infoStream.WriteError($"Failed to load schema: {schemaPath}.", ex);
        }
    }
}
