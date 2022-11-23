using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IStorageCutoff
    {
        IEnumerable<Change> Filter(IEnumerable<Change> changes);
    }

    public class StorageCutoff : IStorageCutoff
    {
        private readonly List<IStorageCutoffFilter> filters;
        public StorageCutoff(List<IStorageCutoffFilter> filters)
        {
            this.filters = filters;
        }

        public IEnumerable<Change> Filter(IEnumerable<Change> changes)
        {
            if (filters.Count < 1)
                return changes;
            return filters.Aggregate(changes, (items, filter) => filter.Filter(items));
        }
    }
}