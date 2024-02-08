using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Data.Storage.Cutoff;
using DotJEM.Web.Scheduler;

namespace DotJEM.Web.Host.Providers.Data.Storage.Indexing;

public interface IJsonStorageAreaObserverFactory
{
    IEnumerable<IJsonStorageAreaObserver> CreateAll();
}

public class JsonStorageAreaObserverFactory : IJsonStorageAreaObserverFactory
{
    private readonly IStorageContext context;
    private readonly IWebTaskScheduler scheduler;
    private readonly IStorageChangeFilterHandler filter;
    private readonly string[] areas;

    public JsonStorageAreaObserverFactory(IStorageContext context, IWebTaskScheduler scheduler, IStorageChangeFilterHandler filter, params string[] areas)
    {
        this.context = context;
        this.scheduler = scheduler;
        this.filter = filter;
        this.areas = areas;
    }

    public IEnumerable<IJsonStorageAreaObserver> CreateAll()
        => areas.Length == 0
            ? context.AreaInfos.Select(areaInfo => new JsonStorageAreaObserver(context.Area(areaInfo.Name), scheduler,filter))
            : areas.Select(area => new JsonStorageAreaObserver(context.Area(area), scheduler,filter));
}
