using System;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management.Info;
using DotJEM.Json.Index2.Management.Observables;
using DotJEM.Json.Index2.Management.Source;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog.ChangeObjects;
using DotJEM.Json.Storage.Adapter.Observable;
using DotJEM.ObservableExtensions.InfoStreams;
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

public class JsonStorageAreaObserver : IJsonStorageAreaObserver
{
    private readonly string pollInterval;
    private readonly IWebTaskScheduler scheduler;
    private readonly IStorageChangeFilterHandler filter;
    private readonly IStorageAreaLog log;
    private readonly DocumentChangesStream observable = new();
    private readonly IInfoStream<JsonStorageAreaObserver> infoStream = new InfoStream<JsonStorageAreaObserver>();

    private long generation = 0;
    private IScheduledTask task;
    public IStorageArea StorageArea { get; }

    public string AreaName => StorageArea.Name;

    public IInfoStream InfoStream => infoStream;
    public IObservable<IJsonDocumentChange> DocumentChanges => observable;
    public IObservableValue<bool> Initialized { get; } = new ObservableValue<bool>();

    public JsonStorageAreaObserver(IStorageArea storageArea, IWebTaskScheduler scheduler, IStorageChangeFilterHandler filter, string pollInterval = "10s")
    {
        StorageArea = storageArea;
        this.scheduler = scheduler;
        this.filter = filter;
        this.pollInterval = pollInterval;
        log = storageArea.Log;
    }

    public async Task RunAsync()
    {
        infoStream.WriteJsonSourceEvent(JsonSourceEventType.Starting, StorageArea.Name, $"Ingest starting for storageArea '{StorageArea.Name}'.");
        task = scheduler.Schedule($"JsonStorageAreaObserver:{StorageArea.Name}", _ => RunUpdateCheck(), pollInterval);
        task.InfoStream.Subscribe(infoStream);
        await task.Signal();
        await task.WhenCompleted();
    }

    public async Task StopAsync()
    {
        task.Dispose();
        await task.WhenCompleted();
        infoStream.WriteJsonSourceEvent(JsonSourceEventType.Stopped, StorageArea.Name, $"Initializing for storageArea '{StorageArea.Name}'.");
    }

    public void UpdateGeneration(string area, long value)
    {
        if(!AreaName.Equals(area))
            return;

        generation = value;
        Initialized.Value = true;
    }

    public async Task QueueUpdate(JObject entity)
    {
        await task.Signal().ConfigureAwait(false);
        //TODO: Wait for completion!
        //return Task.CompletedTask;
    }

    public async Task QueueDelete(JObject entity)
    {
        await task.Signal().ConfigureAwait(false);
        //TODO: Wait for completion!
        //return Task.CompletedTask;
    }
    public Task ResetAsync()
    {
        generation = 0;
        task.Signal();
        return Task.CompletedTask;
    }

    public void RunUpdateCheck()
    {
        long latestGeneration = log.LatestGeneration;
        if (!Initialized.Value)
        {
            BeforeInitialize();
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Initializing, StorageArea.Name, $"Initializing for storageArea '{StorageArea.Name}'.");
            using IStorageAreaLogReader changes = log.OpenLogReader(generation, Initialized.Value);
            PublishChanges(changes, _ => JsonChangeType.Create);
            Initialized.Value = true;
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Initialized, StorageArea.Name, $"Initialization complete for storageArea '{StorageArea.Name}'.");
            AfterInitialize();
        }
        else
        {
            BeforeUpdate();
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Updating, StorageArea.Name, $"Checking updates for storageArea '{StorageArea.Name}'.");
            using IStorageAreaLogReader changes = log.OpenLogReader(generation, Initialized.Value);
            PublishChanges(changes, row => MapChange(row.Type));
            infoStream.WriteJsonSourceEvent(JsonSourceEventType.Updated, StorageArea.Name, $"Done checking updates for storageArea '{StorageArea.Name}'.");
            AfterUpdate();
        }

        JsonChangeType MapChange(ChangeType type)
        {
            return type switch
            {
                ChangeType.Create => JsonChangeType.Create,
                ChangeType.Update => JsonChangeType.Update,
                ChangeType.Delete => JsonChangeType.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        void PublishChanges(IStorageAreaLogReader changes, Func<IChangeLogRow, JsonChangeType> changeTypeGetter)
        {
            foreach (IChangeLogRow change in changes)
            {
                generation = change.Generation;
                if (change.Type == ChangeType.Faulty)
                    continue;

                if(filter.Exclude(change))
                    continue;

                observable.Publish(new JsonDocumentChange(change.Area, changeTypeGetter(change), change.CreateEntity(), change.Size, new GenerationInfo(change.Generation, latestGeneration)));
            }
        }
    }

    public virtual void BeforeInitialize() { }
    public virtual void AfterInitialize() { }

    public virtual void BeforeUpdate() { }
    public virtual void AfterUpdate() { }
}
