using System;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IStorageIndexChangeLogWatcher
    {
        Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
        Task<IStorageChangeCollection> Update(ILuceneWriter writer);
    }

    public class StorageChangeLogWatcher : IStorageIndexChangeLogWatcher
    {
        private readonly string area;
        private readonly int batch;
        private readonly IStorageAreaLog log;
        //private readonly IStorageIndex index;
        public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch)
        {
            this.area = area;
            this.log = log;
            this.batch = batch;
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
                if (changes.Count > 0)
                {
                    writer.WriteAll(changes.Created.Select(change => change.CreateEntity()));
                    writer.WriteAll(changes.Updated.Select(change => change.CreateEntity()));
                    writer.DeleteAll(changes.Deleted.Select(change => change.CreateEntity()));
                }
                return changes;
            });
        }
    }
}