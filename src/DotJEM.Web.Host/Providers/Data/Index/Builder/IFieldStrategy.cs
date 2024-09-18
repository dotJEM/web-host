using System;
using System.Linq;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Data.Index.Builder
{
   public interface IFieldStrategy
    {
        IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type);

    }

    public class FieldStrategy : IFieldStrategy
    {

        public virtual IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new FieldQueryBuilder(parser, fieldName, type);
        }
    }

    public class NullFieldStrategy : FieldStrategy
    {
    }

    public class TermFieldStrategy : FieldStrategy
    {

        //TODO: Select Builder implementation pattern instead.
        public override IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new TermFieldQueryBuilder(parser, fieldName, type);
        }
    }

    public class NumericFieldStrategy : FieldStrategy
    {
        
    }
}
