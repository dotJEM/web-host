using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Index.Builder
{
    public interface IRangeFieldQueryFactory
    {
        Query Create(string field, CallContext call, string part1, string part2, bool startInclusive, bool endInclusive);
    }
}