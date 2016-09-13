using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Searching;

namespace Demo.Server
{
    public class LeoSearchResult
    {
        public long Take { get; set; }
        public long Skip { get; set; }
        public long Count { get; set; }

        public IEnumerable<object> Results { get; set; }

        public LeoSearchResult(ISearchResult result, long take, long skip)
        {
            Take = take;
            Skip = skip;
            Results = result.Select<IHit, object>(hit => hit.Json).ToArray();
            Count = result.TotalCount;
        }
    }
}