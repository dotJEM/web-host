using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
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
        Task Reset(ILuceneWriteContext writer, long generation, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
    }

    public class StorageChangeLogWatcher : IStorageIndexChangeLogWatcher
    {
        private readonly string area;
        private readonly int batch;
        private readonly IStorageAreaLog log;
        private readonly IDiagnosticsLogger logger;
        private readonly IStorageIndexManagerInfoStream info;

        public long Generation => log.CurrentGeneration;

        //private readonly IStorageIndex index;
        public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch, IDiagnosticsLogger logger, IStorageIndexManagerInfoStream infoStream)
        {
            this.area = area;
            this.log = log;
            this.batch = batch;
            this.logger = logger;
            this.info = infoStream;
        }

        public async Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            progress = progress ?? new Progress<StorageIndexChangeLogWatcherInitializationProgress>();
            await Task.Run(async () =>
            {
                while (true)
                {
                    IStorageChangeCollection changes = log.Get(false, batch);
                    if (changes.Count < 1)
                    {
                        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, true));
                        return;
                    }
                    await writer.WriteAll(changes.Partitioned.Select(change => change.CreateEntity()));

                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, false));
                }
            });
        }

        public async Task Reset(ILuceneWriteContext writer, long generation, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            //TODO: This method is almost identical to the one above except for a few things that could be parameterized.
            progress = progress ?? new Progress<StorageIndexChangeLogWatcherInitializationProgress>();

            log.Get(generation, true, 0); //NOTE: Reset to the generation but don't fetch any changes yet.
            progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, new ChangeCount(0, 0, 0), generation, false));

            await Task.Run(async () =>
            {
                while (true)
                {
                    IStorageChangeCollection changes = log.Get(true, batch);
                    if (changes.Count < 1)
                    {
                        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, true));
                        return;
                    }

                    await writer.WriteAll(changes.Created.Select(change => change.CreateEntity()));
                    await writer.WriteAll(changes.Updated.Select(change => change.CreateEntity()));
                    await writer.DeleteAll(changes.Deleted.Select(change => change.CreateEntity()));

                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Generation, false));
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

                writer.WriteAll(changes.Created.Select(change => change.CreateEntity()));
                writer.WriteAll(changes.Updated.Select(change => change.CreateEntity()));
                writer.DeleteAll(changes.Deleted.Select(change => change.CreateEntity()));

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