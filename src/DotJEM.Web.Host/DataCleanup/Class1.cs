using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Providers.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Searching;
using System.Web.Http.Results;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Host.Providers.Concurrency;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StorageConfiguration = DotJEM.Json.Storage.Configuration.StorageConfiguration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Abstractions;

namespace DotJEM.Web.Host.DataCleanup;
public class AbstractionsInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IDataCleanupManager>().ImplementedBy<DataCleanupManager>().LifestyleSingleton());
    }
}

public interface IDataCleanupManager
{
    void Start();
    void Stop();

}

public class DataCleanupManager : IDataCleanupManager
{
    private readonly List<IDataCleaner> cleaners;

    public DataCleanupManager(IStorageIndexManager index, IStorageContext storage, IWebScheduler scheduler, IWebHostConfiguration configuration)
    {
        if (configuration.Cleanup == null)
        {
            cleaners = new List<IDataCleaner>();
            return;
        }

        cleaners = configuration.Cleanup.Items
            .Select(item => (IDataCleaner)new DataCleaner(index, storage, scheduler, item.Query, item.Interval ?? configuration.Cleanup.Interval))
            .ToList();
    }

    public void Start()
    {
        foreach (IDataCleaner cleaner in cleaners)
            cleaner.Start();
    }

    public void Stop()
    {
        foreach (IDataCleaner cleaner in cleaners)
            cleaner.Stop();
    }
}

public interface IDataCleaner
{
    void Start();
    void Stop();
}

public class DataCleaner : IDataCleaner
{
    private readonly IStorageIndexManager indexManager;
    private readonly IStorageContext storage;
    private readonly IWebScheduler scheduler;
    private readonly string query;
    private readonly string expression;

    private readonly Lazy<(string AreaField, string IdField)> configs;
    public  IInfoStream InfoStream { get; } = new DefaultInfoStream<DataCleaner>();

    private IScheduledTask task;

    public DataCleaner(IStorageIndexManager indexManager, IStorageContext storage, IWebScheduler scheduler, string query, string expression)
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
        ISearchResult result = indexManager.Index.Search(query);

        if (result == null)
            return;

        (string areaField, string idField) = configs.Value;
        foreach (IGrouping<string, JObject> group in result.Take(500).Select(hit => hit.Entity).GroupBy(GroupKeySelector))
        {
            if(group.Key == string.Empty)
                continue;

            NewFunction(storage.Area(group.Key), group.AsEnumerable());
        }

        void NewFunction(IStorageArea area, IEnumerable<JObject> items)
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
