using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management.Info;
using DotJEM.Json.Index2.Management.Observables;
using DotJEM.Json.Index2.Management.Source;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog.ChangeObjects;
using DotJEM.Json.Storage.Adapter.Observable;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Providers.Data.Storage.Cutoff;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Data.Storage.Indexing;

public interface IJsonStorageAreaObserver : IJsonDocumentSource
{
    string AreaName { get; }
    Task QueueUpdate(JObject entity);
    Task QueueDelete(JObject entity);
}
internal class Atomic<T>
{
    private T value;
    private readonly object padlock = new();

    public Atomic(T value)
    {
        this.value = value;
    }

    public T Read()
    {
        lock (padlock)
        {
            return value;
        }
    }

    public T Exchange(T value)
    {
        lock (padlock)
        {
            T current = this.value;
            this.value = value;
            return current;
        }
    }

    public static implicit operator Atomic<T>(T value) => new(value);
    public static implicit operator T(Atomic<T> value) => value.Read();
}

public class JsonStorageAreaObserver : IJsonStorageAreaObserver
{
    private readonly string pollInterval;
    private readonly IWebTaskScheduler scheduler;
    private readonly IStorageChangeFilterHandler filter;
    private readonly IStorageAreaLog log;
    private readonly DocumentChangesStream observable = new();
    private readonly IInfoStream<JsonStorageAreaObserver> infoStream = new InfoStream<JsonStorageAreaObserver>();
    private readonly Atomic<bool> started = false;

    private long generation = 0;
    private long initialGeneration = 0;
    private IScheduledTask task;
    public IStorageArea StorageArea { get; }

    public string AreaName => StorageArea.Name;

    public IInfoStream InfoStream => infoStream;
    public IObservable<IJsonDocumentSourceEvent> DocumentChanges => observable;
    public IObservableValue<bool> Initialized { get; } = new ObservableValue<bool>();

    public JsonStorageAreaObserver(
        IStorageArea storageArea,
        IWebTaskScheduler scheduler,
        IStorageChangeFilterHandler filter,
        string pollInterval = "10s")
    {
        StorageArea = storageArea;
        this.scheduler = scheduler;
        this.filter = filter;
        this.pollInterval = pollInterval;
        log = storageArea.Log;
    }

    public async Task StartAsync()
    {
        if(started.Exchange(true))
            return;
        infoStream.WriteJsonSourceEvent(JsonSourceEventType.Starting, StorageArea.Name, $"Ingest starting for storageArea '{StorageArea.Name}'.");
        task = scheduler.Schedule($"JsonStorageAreaObserver:{StorageArea.Name}", _ => RunUpdateCheck(), pollInterval);
        task.InfoStream.Subscribe(infoStream);
        await task.Signal().ConfigureAwait(false);
    }



    public async Task StopAsync()
    {
        if (!started.Exchange(false))
            return;
        
        task.Dispose();
        await task.WhenCompleted().ConfigureAwait(false);
        infoStream.WriteJsonSourceEvent(JsonSourceEventType.Stopped, StorageArea.Name, $"Initializing for storageArea '{StorageArea.Name}'.");
    }

    public void UpdateGeneration(string area, long value)
    {
        if(!AreaName.Equals(area))
            return;

        infoStream.WriteDebug($"[{area}] Setting generation to {value}");
        generation = value;
        initialGeneration = value;
        Initialized.Value = true;
    }

    public async Task QueueUpdate(JObject entity)
    {
        infoStream.WriteDebug($"[{AreaName}] QueueUpdate.");
        await task.Signal().ConfigureAwait(false);
    }

    public async Task QueueDelete(JObject entity)
    {
        infoStream.WriteDebug($"[{AreaName}] QueueDelete.");
        await task.Signal().ConfigureAwait(false);
    }
    public async Task ResetAsync()
    {
        infoStream.WriteDebug($"[{AreaName}] Resetting storage area observer.");
        UpdateGeneration(AreaName, initialGeneration);
        observable.Publish(new JsonDocumentSourceReset(AreaName));
    }

    public void RunUpdateCheck()
    {
        long latestGeneration = log.LatestGeneration;
        if (!Initialized.Value)
        {
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Initializing, StorageArea.Name, $"Initializing for storageArea '{StorageArea.Name}'.");
            using IStorageAreaLogReader changes = log.OpenLogReader(generation, false);
            PublishChanges(changes, row => new JsonDocumentCreated(row.Area, row.CreateEntity(), row.Size, new(row.Generation, latestGeneration)));
            Initialized.Value = true;
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Initialized, StorageArea.Name, $"Initialization complete for storageArea '{StorageArea.Name}'.");
        }
        else
        {
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Updating, StorageArea.Name, $"Checking updates for storageArea '{StorageArea.Name}'.");
            using IStorageAreaLogReader changes = log.OpenLogReader(generation);
            PublishChanges(changes, MapRow);
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Updated, StorageArea.Name, $"Done checking updates for storageArea '{StorageArea.Name}'.");
        }
        observable.Publish(new JsonDocumentSourceDigestCompleted(AreaName));

        IJsonDocumentSourceEvent MapRow(IChangeLogRow row)
        {
            return row.Type switch
            {
                ChangeType.Create => new JsonDocumentCreated(row.Area, row.CreateEntity(), row.Size, new(row.Generation, latestGeneration)),
                ChangeType.Update => new JsonDocumentUpdated(row.Area, row.CreateEntity(), row.Size, new(row.Generation, latestGeneration)),
                ChangeType.Delete => new JsonDocumentDeleted(row.Area, row.CreateEntity(), row.Size, new(row.Generation, latestGeneration)),
                _ => throw new NotSupportedException()
            };
        }
        
        void PublishChanges(IStorageAreaLogReader changes, Func<IChangeLogRow, IJsonDocumentSourceEvent> rowMapper)
        {
            foreach (IChangeLogRow change in changes)
            {
                generation = change.Generation;
                if (change.Type == ChangeType.Faulty)
                    continue;

                if(filter.Exclude(change))
                    continue;
                
                observable.Publish(rowMapper(change));
            }
        }
    }
}
