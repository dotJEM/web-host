using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog.ChangeObjects;

namespace DotJEM.Web.Host.Providers.Data.Storage.Cutoff;
public interface IStorageChangeFilter
{
    bool Exclude(IChangeLogRow change);
}

public interface IStorageChangeFilterHandler
{
    bool Exclude(IChangeLogRow change);
}

public class StorageChangeFilterHandler : IStorageChangeFilterHandler
{
    private readonly List<IStorageChangeFilter> filters;

    public StorageChangeFilterHandler(params IStorageChangeFilter[] filters)
    {
        this.filters = filters.ToList();
    }

    //public IEnumerable<Change> Filter(IEnumerable<Change> changes)
    //{
    //    if (filters.Count < 1)
    //        return changes;
    //    return filters.Aggregate(changes, (items, filter) => filter.Filter(items));
    //}

    public bool Exclude(IChangeLogRow change)
    {
        return filters.Any(filter => filter.Exclude(change));
    }
}