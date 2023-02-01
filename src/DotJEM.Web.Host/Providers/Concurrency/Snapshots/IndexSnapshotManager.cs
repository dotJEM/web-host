using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.MetadataServices;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public interface IIndexSnapshotManager
{
    IInfoStream InfoStream { get; }

    void ReplaceStrategy(ISnapshotStrategy strategy);
    bool TakeSnapshot();
    bool RestoreSnapshot();

    void Pause();
    void Resume();
    void Start();
    void Stop();
}

public class IndexSnapshotManager : IIndexSnapshotManager
{
    private bool paused = false;
    private bool started = false;
    private readonly IStorageIndex index;
    private readonly IStorageContext storage;
    private readonly IWebHostConfiguration configuration;
    private readonly IWebScheduler scheduler;
    private readonly IDiagnosticsLogger logger;

    private readonly int maxSnapshots;
    private ISnapshotStrategy strategy;
    private IScheduledTask task;

    public IInfoStream InfoStream { get; } = new DefaultInfoStream<IndexSnapshotManager>();

    public IndexSnapshotManager(
        IStorageIndex index,
        IStorageContext storage,
        IWebHostConfiguration configuration, 
        IPathResolver path,
        IWebScheduler scheduler,
        IDiagnosticsLogger logger)
    {
        this.index = index;
        this.storage = storage;
        this.configuration = configuration;
        this.scheduler = scheduler;
        this.logger = logger;
        this.maxSnapshots = configuration.Index.Snapshots.MaxSnapshots;
        this.strategy = maxSnapshots > 0 ? new ZipSnapshotStrategy(path.MapPath(configuration.Index.Snapshots.Path)) : null;
        this.strategy?.InfoStream.Forward(this.InfoStream);
    }

    public void Start()
    {
        if(started) return;
        
        started = true;
        if (!string.IsNullOrEmpty(configuration.Index.Snapshots.CronTime))
            this.task =scheduler.ScheduleCron("Snapshot-Schedule", _ => TakeSnapshot(), configuration.Index.Snapshots.CronTime);
    }

    public void Stop()
    {
        task.Dispose();
    }

    public void ReplaceStrategy(ISnapshotStrategy strategy)
    {
        this.strategy = strategy;
    }

    public bool TakeSnapshot()
    {
        if(paused || maxSnapshots <= 0 || strategy == null) return false;
            
        JObject generations = storage.AreaInfos
            .Aggregate(new JObject(), (x, info) => {
                x[info.Name] = storage.Area(info.Name).Log.CurrentGeneration;
                return x;
            });

        try
        {
            ISnapshotTarget target = strategy.CreateTarget(new JObject { ["storageGenerations"] = generations });
            index.Commit();
            index.Storage.Snapshot(target);
            InfoStream.WriteInfo($"Created snapshot");
        }
        catch (Exception exception)
        {
            logger.Log("failure", Severity.Error, "Failed to take snapshot.", new { exception });
        }
        strategy.CleanOldSnapshots(maxSnapshots);
        return true;
    }

    public bool RestoreSnapshot()
    {
        if(maxSnapshots <= 0 || strategy == null) return false;

        int offset = 0;
        while (true)
        {
            try
            {
                ISnapshotSourceWithMetadata source = strategy.CreateSource(offset++);
                if (source == null)
                    return false;

                if (!source.Verify())
                {
                    source.Delete();
                    continue;
                }

                index.Storage.Restore(source);
                if (source.Metadata["storageGenerations"] is not JObject metadata) continue;
                    
                foreach (JProperty property in metadata.Properties())
                    storage.Area(property.Name).Log.Get(property.Value.ToObject<long>(), count: 0);

                return true;
            }
            catch (Exception ex)
            {
                InfoStream.WriteError("Failed to restore snapshot.", ex);
            }
        }
    }

    public void Pause()
    {
        paused = true;
    }

    public void Resume()
    {
        paused = false;
    }
}