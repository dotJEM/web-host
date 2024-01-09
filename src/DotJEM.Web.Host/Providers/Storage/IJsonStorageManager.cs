using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Storage.Cutoff;
using DotJEM.Web.Host.Providers.Storage.Indexing;
using DotJEM.Web.Host.Tasks;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Storage;

public interface IJsonStorageManager
{
    IReadOnlyCollection<IJsonStorageAreaObserver> Observers { get; }
    IJsonDocumentSource DocumentSource { get; }
    void Start();
    void Stop();
    Task QueueUpdate(IStorageArea area, JObject entity);
    Task QueueDelete(IStorageArea area, JObject deleted);
}

public class JsonStorageManager : IJsonStorageManager
{
    private IScheduledTask task;
    private readonly IWebTaskScheduler scheduler;
    private readonly IDiagnosticsLogger logger;
    private readonly Dictionary<string, IStorageHistoryCleaner> cleaners = new();
    private readonly TimeSpan interval;
    private readonly JsonStorageDocumentSource documentSource;

    public IReadOnlyCollection<IJsonStorageAreaObserver> Observers { get; }
    public IJsonDocumentSource DocumentSource => documentSource;

    public JsonStorageManager(
        IStorageContext storage,
        IWebHostConfiguration configuration,
        IWebTaskScheduler scheduler,
        IStorageChangeFilterHandler filter, 
        IDiagnosticsLogger logger)
    {
        this.scheduler = scheduler;
        this.logger = logger;
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


        AreaWatchElement[] watch = configuration.Index.Watch.Items
            .Select(AreaWatchElement.Create)
            .ToArray();

        Observers = storage.AreaInfos
            .Select(area =>
            {
                AreaWatchElement match = watch.FirstOrDefault(x => x.IsMatch(area.Name));
                return match == null 
                    ? (IJsonStorageAreaObserver)null 
                    : new JsonStorageAreaObserver(storage.Area(area.Name), scheduler, filter);
            })
            .Where(observer => observer != null)
            .ToList()
            .AsReadOnly();

        documentSource = new JsonStorageDocumentSource(Observers);
    }

    public async Task  QueueUpdate(IStorageArea area, JObject entity)
    {
        await documentSource.QueueUpdate(area, entity);
    }

    public async Task QueueDelete(IStorageArea area, JObject deleted)
    {
        await documentSource.QueueDelete(area, deleted);
    }

    public void Start()
    {
        task = scheduler.ScheduleTask("StorageManager.CleanHistory", b => CleanHistory(), interval);
        //foreach (IJsonStorageAreaObserver observer in Observers)
        //{
        //    Sync.FireAndForget(observer.RunAsync(), exception => logger.LogException(exception));
        //}
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

public class AreaWatchElement
{
    private readonly Regex nameMatch;
    private readonly int batchSize;
    private readonly long initialGeneration;

    private AreaWatchElement(Regex nameMatch, int batchSize, long initialGeneration)
    {
        this.nameMatch = nameMatch;
        this.batchSize = batchSize;
        this.initialGeneration = initialGeneration;
    }

    public bool IsMatch(string area) => nameMatch.IsMatch(area);

    public static AreaWatchElement Create(WatchElement arg)
    {
        return new AreaWatchElement(WildcardToRegex(arg.Area), arg.BatchSize, arg.InitialGeneration);
        Regex WildcardToRegex(string pattern)
        {
            return new Regex( "^" + Regex.Escape(pattern).
                Replace("\\*", ".*").
                Replace("\\?", ".") + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}