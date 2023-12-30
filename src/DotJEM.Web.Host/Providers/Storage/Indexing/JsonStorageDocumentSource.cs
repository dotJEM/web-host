using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Storage.Indexing;

public class JsonStorageDocumentSource : IJsonDocumentSource
{
    private readonly Dictionary<string, IJsonStorageAreaObserver> observers;
    private readonly ChangeStream observable = new();
    private readonly InfoStream<JsonStorageDocumentSource> infoStream = new();

    public IObservable<IJsonDocumentChange> Observable => observable;
    public IInfoStream InfoStream => infoStream;

    //public JsonStorageDocumentSource(IStorageContext context, IWebTaskScheduler scheduler)
    //    : this(new JsonStorageAreaObserverFactory(context, scheduler))
    //{
    //}

    public JsonStorageDocumentSource(IJsonStorageAreaObserverFactory factory)
        : this(factory.CreateAll()) {}

    public JsonStorageDocumentSource(params IJsonStorageAreaObserver[] observers)
        : this(observers.AsEnumerable()) {}

    public JsonStorageDocumentSource(IEnumerable<IJsonStorageAreaObserver> observers)
    {
        this.observers = observers.Select(observer =>
        {
            observer.Observable.Subscribe(observable);
            observer.InfoStream.Subscribe(infoStream);
            return observer;
        }).ToDictionary(x => x.AreaName);
    }

    public async Task RunAsync()
    {
        await Task.WhenAll(
            observers.Values.Select(async observer => await observer.RunAsync().ConfigureAwait(false))
        ).ConfigureAwait(false);
    }

    public void UpdateGeneration(string area, long generation)
    {
        if (!observers.TryGetValue(area, out IJsonStorageAreaObserver observer))
            return; // TODO?

        observer.UpdateGeneration(generation);
    }

    public async Task QueueUpdate(IStorageArea area, JObject entity)
    {
        if(!observers.TryGetValue(area.Name, out IJsonStorageAreaObserver observer))
            return;

        await observer.QueueUpdate(entity).ConfigureAwait(false);

    }

    public async Task QueueDelete(IStorageArea area, JObject deleted)
    {
        if(!observers.TryGetValue(area.Name, out IJsonStorageAreaObserver observer))
            return;

        await observer.QueueDelete(deleted).ConfigureAwait(false);
    }
}