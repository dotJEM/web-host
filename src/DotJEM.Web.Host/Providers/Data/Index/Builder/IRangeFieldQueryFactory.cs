using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Data.Index.Builder
{
    public interface IRangeFieldQueryFactory
    {
        Query Create(string field, CallContext call, string part1, string part2, bool startInclusive, bool endInclusive);
    }
}