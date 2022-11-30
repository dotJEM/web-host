using System;
using System.Collections.Generic;
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

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IStorageIndexChangeLogWatcher
    {
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
        private readonly IDiagnosticsLogger logger;
        private readonly IStorageCutoff cufoff;
        private readonly IStorageIndexManagerInfoStream info;

        public long Generation => log.CurrentGeneration;

        public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch, long initialGeneration, IStorageCutoff cufoff,  IDiagnosticsLogger logger, IStorageIndexManagerInfoStream infoStream)
        {
            this.area = area;
            this.log = log;
            this.batch = batch;
            this.initialGeneration = initialGeneration;
            this.logger = logger;
            this.cufoff = cufoff;
            this.info = infoStream;
        }

        private void SetInitialGeneration()
        {
            log.Get(initialGeneration, true, 0);
        }

        public async Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            progress ??= new Progress<StorageIndexChangeLogWatcherInitializationProgress>();
            await Task.Run(async () =>
            {
                SetInitialGeneration();
                long latest = log.LatestGeneration;
                long writeCount = 0;
                while (true)
                {
                    IStorageChangeCollection changes = log.Get(false, batch);
                    if (changes.Count < 1)
                    {
                        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, true));
                        return;
                    }
                    await writer.WriteAll(cufoff.Filter(changes.Partitioned).Select(change => change.CreateEntity()));
                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, false));
                }
            });
        }

        public async Task Reset(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            await Reset(writer, initialGeneration, progress);
        }

        public async Task Reset(ILuceneWriteContext writer, long generation, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            //TODO: This method is almost identical to the one above except for a few things that could be parameterized.
            progress = progress ?? new Progress<StorageIndexChangeLogWatcherInitializationProgress>();

            log.Get(generation, true, 0); //NOTE: Reset to the generation but don't fetch any changes yet.
            long latest = log.LatestGeneration;
            progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, new ChangeCount(0, 0, 0), generation, latest, false));

            await Task.Run(async () =>
            {
                while (true)
                {
                    IStorageChangeCollection changes = log.Get(true, batch);
                    if (changes.Count < 1)
                    {
                        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, true));
                        return;
                    }

                    await writer.WriteAll(cufoff.Filter(changes.Created).Select(change => change.CreateEntity()));
                    await writer.WriteAll(cufoff.Filter(changes.Updated).Select(change => change.CreateEntity()));
                    await writer.DeleteAll(cufoff.Filter(changes.Deleted).Select(change => change.CreateEntity()));

                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, latest, false));
                }
            });
        }

        public async Task<IStorageChangeCollection> Update(ILuceneWriter writer)
        {
            return await Task.Run(() =>
            {
                IStorageChangeCollection changes = log.Get(count: batch);
                if (changes.Count <= 0)
                    return changes;

                writer.WriteAll(cufoff.Filter(changes.Created).Select(change => change.CreateEntity()));
                writer.WriteAll(cufoff.Filter(changes.Updated).Select(change => change.CreateEntity()));
                writer.DeleteAll(cufoff.Filter(changes.Deleted).Select(change => change.CreateEntity()));

                info.Publish(changes);

                List<FaultyChange> faults = changes.OfType<FaultyChange>().ToList();
                if (faults.Any())
                {
                    info.Record(area, faults);
                    logger.LogFailure(Severity.Critical, "Faulty objects discovered in the database: ", new { faults } );
                }

                info.Track(area,changes.Count.Created, changes.Count.Updated, changes.Count.Deleted, faults.Count);

                return changes;
            });
        }
    }
}