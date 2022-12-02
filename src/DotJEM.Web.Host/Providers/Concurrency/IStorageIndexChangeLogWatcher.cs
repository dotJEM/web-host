using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

namespace DotJEM.Web.Host.Providers.Concurrency;

public interface IStorageIndexChangeLogWatcher
{
    IInfoStream InfoStream { get; }
    long Generation { get; }
    Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
    Task<IStorageChangeCollection> Update(ILuceneWriter writer);
    Task Reset(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
    Task Reset(ILuceneWriteContext writer, long generation, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
}

public class StorageChangeLogWatcher : IStorageIndexChangeLogWatcher
{
    private readonly string area;
    private readonly int batch;
    private readonly long initialGeneration;
    private readonly IStorageAreaLog log;
    private readonly IStorageCutoff cufoff;
    public IInfoStream InfoStream { get; } = new DefaultInfoStream<StorageChangeLogWatcher>();
    public long Generation => log.CurrentGeneration;

    public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch, long initialGeneration, IStorageCutoff cufoff)
    {
        this.area = area;
        this.log = log;
        this.batch = batch;
        this.initialGeneration = initialGeneration;
        this.cufoff = cufoff;
    }

    private void SetInitialGeneration()
    {
        log.Get(initialGeneration, true, 0);
    }

    public Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
    {
        progress ??= new Progress<StorageIndexChangeLogWatcherInitializationProgress>();
        return Task.Run(async () =>
        {
            SetInitialGeneration();
            long latest = log.LatestGeneration;
            progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, new ChangeCount(), 0, latest, false));
            InfoStream.WriteIndexStarting(area, initialGeneration, latest);

            IStorageChangeCollection changes;
            while ((changes = log.Get(false, batch)).Count > 0)
            {
                await writer.WriteAll(cufoff.Filter(changes.Partitioned).Select(change => change.CreateEntity()));
                InfoStream.WriteIndexIngest(changes);
                progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, false));
            }
            progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, true));
            InfoStream.WriteIndexInitialized(area, changes.Generation);

            //while (true)
            //{
            //    IStorageChangeCollection changes = log.Get(false, batch);
            //    if (changes.Count < 1)
            //    {
            //        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, true));
            //        InfoStream.WriteIndexInitialized(area, changes.Generation);
            //        return;
            //    }
            //    await writer.WriteAll(cufoff.Filter(changes.Partitioned).Select(change => change.CreateEntity()));
            //    InfoStream.WriteIndexIngest(changes);
            //    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, false));
            //}
        });
    }

    public async Task Reset(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
    {
        await Reset(writer, initialGeneration, progress);
    }

    public Task Reset(ILuceneWriteContext writer, long generation, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
    {
        //TODO: This method is almost identical to the one above except for a few things that could be parameterized.
        progress = progress ?? new Progress<StorageIndexChangeLogWatcherInitializationProgress>();

        return Task.Run(async () =>
        {
            log.Get(generation, true, 0); //NOTE: Reset to the generation but don't fetch any changes yet.
            long latest = log.LatestGeneration;
            progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, new ChangeCount(0, 0, 0), generation, latest, false));
            InfoStream.WriteIndexStarting(area, initialGeneration, latest);

            while (true)
            {
                IStorageChangeCollection changes = log.Get(true, batch);
                if (changes.Count < 1)
                {
                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, true));
                    InfoStream.WriteIndexInitialized(area, changes.Generation);
                    return;
                }

                await writer.WriteAll(cufoff.Filter(changes.Created).Select(change => change.CreateEntity())).ConfigureAwait(false);
                await writer.WriteAll(cufoff.Filter(changes.Updated).Select(change => change.CreateEntity())).ConfigureAwait(false);
                await writer.DeleteAll(cufoff.Filter(changes.Deleted).Select(change => change.CreateEntity())).ConfigureAwait(false);
                InfoStream.WriteIndexIngest(changes);
                progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, false));
            }
        });
    }

    public Task<IStorageChangeCollection> Update(ILuceneWriter writer)
    {
        return Task.Run(() =>
        {
            IStorageChangeCollection changes = log.Get(count: batch);
            if (changes.Count <= 0)
                return changes;

            writer.WriteAll(cufoff.Filter(changes.Created).Select(change => change.CreateEntity()));
            writer.WriteAll(cufoff.Filter(changes.Updated).Select(change => change.CreateEntity()));
            writer.DeleteAll(cufoff.Filter(changes.Deleted).Select(change => change.CreateEntity()));
            InfoStream.WriteIndexIngest(changes);
            return changes;
        });
    }
}

public static class StorageChangeLogWatcherExtensions
{
    public static void WriteIndexIngest(this IInfoStream self, IStorageChangeCollection changes)
        => self.WriteEvent(new IndexIngestEvent(changes));

    public static void WriteIndexStarting(this IInfoStream self, string area, long initialGeneration, long latestGeneration)
        => self.WriteEvent(new IndexStartingEvent(area, initialGeneration, latestGeneration));

    public static void WriteIndexInitialized(this IInfoStream self, string area, long generation)
        => self.WriteEvent(new IndexInitializedEvent(area, generation));

}

public class IndexStartingEvent : IInfoStreamEvent
{
    public string Level => "START";
    public string Message { get; }
    public IndexStartingEvent(string area, long initialGeneration, long latestGeneration)
    {
    }
}
public class IndexInitializedEvent : IInfoStreamEvent
{
    public string Level => "INITIALIZED";
    public string Message { get; }
    public IndexInitializedEvent(string area, long generation)
    {
    }
}

public class IndexIngestEvent : IInfoStreamEvent
{
    private readonly IStorageChangeCollection changes;

    public string Level => "INGEST";
    public string Message { get; }

    public IndexIngestEvent(IStorageChangeCollection changes)
    {
        this.changes = changes;
    }
}