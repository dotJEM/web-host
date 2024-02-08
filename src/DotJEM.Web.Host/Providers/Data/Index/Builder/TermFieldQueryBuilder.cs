using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Data.Index.Builder
{
    public class TermFieldQueryBuilder : FieldQueryBuilder
    {
        public TermFieldQueryBuilder(IQueryParser parser, string field, JsonSchemaExtendedType type)
            : base(parser, field, type)
        {
        }

        public override Query BuildFieldQuery(CallContext call, string query, int slop)
        {
            return new TermQuery(new Term(Field, query));
        }
    }
}