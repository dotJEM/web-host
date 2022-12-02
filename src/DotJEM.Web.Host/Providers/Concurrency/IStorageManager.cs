using DotJEM.Json.Index.Util;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Providers.Scheduler;
using System.Collections.Generic;
using System;

namespace DotJEM.Web.Host.Providers.Concurrency;

public interface IStorageManager
{
    void Start();
    void Stop();
}  
    
public class StorageManager : IStorageManager
{
    private IScheduledTask task;
    private readonly IWebScheduler scheduler;
    private readonly Dictionary<string, IStorageHistoryCleaner> cleaners = new Dictionary<string, IStorageHistoryCleaner>();
    private readonly TimeSpan interval;

    public StorageManager(IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler)
    {
        this.scheduler = scheduler;
        this.interval = AdvConvert.ConvertToTimeSpan(configuration.Storage.Interval);
        foreach (StorageAreaElement areaConfig in configuration.Storage.Items)
        {
            IStorageAreaConfigurator areaConfigurator = storage.Configure.Area(areaConfig.Name);
            if (!areaConfig.History)
                continue;

            areaConfigurator.EnableHistory();
            if (string.IsNullOrEmpty(areaConfig.HistoryAge))
                continue;

            TimeSpan historyAge = AdvConvert.ConvertToTimeSpan(areaConfig.HistoryAge);
            if(historyAge <= TimeSpan.Zero)
                continue;

            cleaners.Add(areaConfig.Name, new StorageHistoryCleaner(
                new Lazy<IStorageAreaHistory>(() => storage.Area(areaConfig.Name).History),
                historyAge));
        }
    }
    public void Start()
    {
        task = scheduler.ScheduleTask("StorageManager.CleanHistory", b => CleanHistory(), interval);
    }

    private void CleanHistory()
    {
        foreach (IStorageHistoryCleaner cleaner in cleaners.Values)
        {
            cleaner.Execute();
        }
    }

    public void Stop() => task.Dispose();

}