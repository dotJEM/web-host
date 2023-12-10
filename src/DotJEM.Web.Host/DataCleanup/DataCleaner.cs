using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Results;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace DotJEM.Web.Host.DataCleanup;

public interface IDataCleaner
{
    void Start();
    void Stop();
}
public class DataCleaner : IDataCleaner
{
    private readonly IJsonIndexManager indexManager;
    private readonly IStorageContext storage;
    private readonly IWebTaskScheduler scheduler;
    private readonly string query;
    private readonly string expression;

    private readonly Lazy<(string AreaField, string IdField)> configs;
    public  IInfoStream InfoStream { get; } = new DefaultInfoStream<DataCleaner>();

    private IScheduledTask task;

    public DataCleaner(IJsonIndexManager indexManager, IStorageContext storage, IWebTaskScheduler scheduler, string query, string expression)
    {
        this.indexManager = indexManager;
        this.storage = storage;
        this.scheduler = scheduler;
        this.query = query;
        this.expression = expression;

        StorageConfiguration config = (StorageConfiguration)storage.Configure;
        configs = new(() => (config.Fields[JsonField.Area], config.Fields[JsonField.Id]));
    }

    public void Start()
    {
        Clean(false);
        this.task = scheduler.Schedule("DataCleaner:" + query, Clean, expression);
    }

    private void Clean(bool obj)
    {
        ISearch result = indexManager.Index.Search(query);

        if (result == null)
            return;

        (string areaField, string idField) = configs.Value;
        foreach (IGrouping<string, JObject> group in result.Take(500)
                     .Select(hit => hit.Entity).GroupBy(GroupKeySelector))
        {
            if(group.Key == string.Empty)
                continue;
            DeleteDocuments(storage.Area(group.Key), group.AsEnumerable());
        }
        indexManager.UpdateIndex();

        void DeleteDocuments(IStorageArea area, IEnumerable<JObject> items)
        {
            foreach (JObject entity in items)
            {
                try
                {
                    area.Delete((Guid)entity[idField]);
                }
                catch (Exception e)
                {
                    InfoStream.WriteError($"Failed to delete: '{entity.ToString(Formatting.None)}'", e);
                }
            }
        }

        string GroupKeySelector(JObject doc)
        {
            try
            {
                return (string)doc[areaField];
            }
            catch (Exception e)
            {
                InfoStream.WriteError($"Failed to get area for: '{doc.ToString(Formatting.None)}'", e);
                return string.Empty;
            }
        }
    }

    public void Stop()
    {
        task.Dispose();
    }
}