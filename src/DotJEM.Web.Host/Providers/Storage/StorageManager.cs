using System;
using System.Collections.Generic;
using DotJEM.AdvParsers;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Scheduler;

namespace DotJEM.Web.Host.Providers.Storage;

public interface IStorageManager
{
    void Start();
    void Stop();
}

public class StorageManager : IStorageManager
{
    private IScheduledTask task;
    private readonly IWebTaskScheduler scheduler;
    private readonly Dictionary<string, IStorageHistoryCleaner> cleaners = new();
    private readonly TimeSpan interval;

    public StorageManager(IStorageContext storage, IWebHostConfiguration configuration, IWebTaskScheduler scheduler)
    {
        this.scheduler = scheduler;
        this.interval = AdvParser.ParseTimeSpan(configuration.Storage.Interval);
        //this.interval = AdvConvert.ConvertToTimeSpan(configuration.Storage.Interval);
        foreach (StorageAreaElement areaConfig in configuration.Storage.Items)
        {
            IStorageAreaConfigurator areaConfigurator = storage.Configure.Area(areaConfig.Name);
            if (!areaConfig.History)
                continue;

            areaConfigurator.EnableHistory();
            if (string.IsNullOrEmpty(areaConfig.HistoryAge))
                continue;

            //TimeSpan historyAge = AdvConvert.ConvertToTimeSpan(areaConfig.HistoryAge);
            TimeSpan historyAge = AdvParser.ParseTimeSpan(areaConfig.HistoryAge);
            if (historyAge <= TimeSpan.Zero)
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