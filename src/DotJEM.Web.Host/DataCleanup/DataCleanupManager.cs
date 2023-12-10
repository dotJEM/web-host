using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Scheduler;

namespace DotJEM.Web.Host.DataCleanup;

public interface IDataCleanupManager
{
    void Start();
    void Stop();

}
public class DataCleanupManager : IDataCleanupManager
{
    private readonly List<IDataCleaner> cleaners;

    public DataCleanupManager(IJsonIndexManager index, IStorageContext storage, IWebTaskScheduler scheduler, IWebHostConfiguration configuration)
    {
        if (configuration.Cleanup == null)
        {
            cleaners = new List<IDataCleaner>();
            return;
        }

        cleaners = configuration.Cleanup.Items
            .Select(item => (IDataCleaner)new DataCleaner(index, storage, scheduler, item.Query, string.IsNullOrEmpty(item.Interval) ? configuration.Cleanup.Interval : item.Interval))
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