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
        IJsonIndexWriter IndexWriter { get; }
        IJsonStorageManager StorageManager { get; }
        Task QueueUpdate(IStorageArea area, JObject entity);
        Task QueueDelete(IStorageArea area, JObject deleted);
    }

    public class DataStorageManager : IDataStorageManager
    {
        private readonly InfoStream<DataStorageManager> infoStream = new InfoStream<DataStorageManager>();

        public IJsonIndexManager IndexManager { get; }
        public IJsonIndexWriter IndexWriter { get; }
        public IJsonStorageManager StorageManager { get; }

        public IInfoStream InfoStream => infoStream;


        public DataStorageManager(IJsonIndexManager indexManager, IJsonIndexWriter indexWriter, IJsonStorageManager storageManager)
        {
            this.IndexManager = indexManager;
            this.IndexWriter = indexWriter;
            this.StorageManager = storageManager;

            indexManager.InfoStream.Subscribe(infoStream);
            indexWriter.InfoStream.Subscribe(infoStream);
            storageManager.InfoStream.Subscribe(infoStream);
        }

        public async Task QueueUpdate(IStorageArea area, JObject entity)
        {
            await StorageManager.QueueUpdate(area, entity);
            //IndexWriter.Commit();
        }

        public async Task QueueDelete(IStorageArea area, JObject deleted)
        {
            await StorageManager.QueueDelete(area, deleted);
            //IndexWriter.Commit();
        }
    }
}
