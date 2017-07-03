using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Web.Host.Diagnostics;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IStorageIndexChangeLogWatcher
    {
        long Generation { get; }
        Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
        Task<IStorageChangeCollection> Update(ILuceneWriter writer);
    }

    public class StorageChangeLogWatcher : IStorageIndexChangeLogWatcher
    {
        private readonly string area;
        private readonly int batch;
        private readonly IStorageAreaLog log;
        private readonly IDiagnosticsLogger logger;

        public long Generation => log.Generation;

        //private readonly IStorageIndex index;
        public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch, IDiagnosticsLogger logger)
        {
            this.area = area;
            this.log = log;
            this.batch = batch;
            this.logger = logger;
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
                        progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Token, true));
                        return;
                    }
                    await writer.WriteAll(changes.Partitioned.Select(change => change.CreateEntity()));

                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Token, false));
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

                List<Change> faults = changes.Where(c => c is FaultyChange).ToList();
                if (faults.Any())
                    logger.LogFailure(Severity.Critical, "Faulty objects discovered in the database: ", new { faults } );
                return changes;
            });
        }
    }
}