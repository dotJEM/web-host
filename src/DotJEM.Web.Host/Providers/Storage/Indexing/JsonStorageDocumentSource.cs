using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Observables;
using DotJEM.Json.Index2.Management.Source;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Storage.Indexing;

public class JsonStorageDocumentSource : IJsonDocumentSource
{
    private readonly Dictionary<string, IJsonStorageAreaObserver> observers;
    private readonly DocumentChangesStream observable = new();
    private readonly InfoStream<JsonStorageDocumentSource> infoStream = new();

    public IObservable<IJsonDocumentChange> DocumentChanges => observable;
    public IInfoStream InfoStream => infoStream;
    public IObservableValue<bool> Initialized { get; } = new ObservableValue<bool>();

    public JsonStorageDocumentSource(IJsonStorageAreaObserverFactory factory)
        : this(factory.CreateAll()) {}

    public JsonStorageDocumentSource(params IJsonStorageAreaObserver[] observers)
        : this(observers.AsEnumerable()) {}

    public JsonStorageDocumentSource(IEnumerable<IJsonStorageAreaObserver> observers)
    {
        this.observers = observers.Select(observer =>
        {
            observer.DocumentChanges.Subscribe(observable);
            observer.InfoStream.Subscribe(infoStream);
            observer.Initialized.Subscribe(_ => InitializedChanged());
            return observer;
        }).ToDictionary(x => x.AreaName);
    }
    
    private void InitializedChanged()
    {
        this.Initialized.Value = observers.Values.All(observer => observer.Initialized.Value);
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

        observer.UpdateGeneration(area, generation);
    }

    public async Task ResetAsync()
    {
        foreach (IJsonStorageAreaObserver observer in observers.Values)
            await observer.ResetAsync().ConfigureAwait(false);
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