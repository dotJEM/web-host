using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Writer;
using DotJEM.Json.Storage.Adapter;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Host.Providers.Data.Storage;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Data
{
    public interface IDataStorageManager
    {
        IInfoStream InfoStream { get; }
        IJsonIndexManager IndexManager { get; }
        IJsonStorageManager StorageManager { get; }
        Task QueueUpdate(IStorageArea area, JObject entity);
        Task QueueDelete(IStorageArea area, JObject deleted);
    }

    public class DataStorageManager : IDataStorageManager
    {
        private readonly InfoStream<DataStorageManager> infoStream = new InfoStream<DataStorageManager>();

        public IJsonIndexManager IndexManager { get; }
        public IJsonStorageManager StorageManager { get; }

        public IInfoStream InfoStream => infoStream;


        public DataStorageManager(IJsonIndexManager indexManager, IJsonStorageManager storageManager)
        {
            this.IndexManager = indexManager;
            this.StorageManager = storageManager;

            indexManager.InfoStream.Subscribe(infoStream);
            storageManager.InfoStream.Subscribe(infoStream);
        }

        public async Task QueueUpdate(IStorageArea area, JObject entity)
        {
            await StorageManager.QueueUpdate(area, entity).ConfigureAwait(false);
        }

        public async Task QueueDelete(IStorageArea area, JObject deleted)
        {
            await StorageManager.QueueDelete(area, deleted).ConfigureAwait(false);
        }
    }
}
