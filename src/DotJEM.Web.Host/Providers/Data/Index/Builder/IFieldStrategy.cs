using System;
using System.Linq;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Data.Index.Builder
{
   public interface IFieldStrategy
    {
        Query BuildQuery(string path, string value);

        IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type);

    }

    public class FieldStrategy : IFieldStrategy
    {

        public virtual IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new FieldQueryBuilder(parser, fieldName, type);
        }

        //NOTE: This is temporary for now.
        private static readonly char[] delimiters = " ".ToCharArray();
        public virtual Query BuildQuery(string field, string value)
        {
            value = value.ToLowerInvariant();
            string[] words = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (!words.Any())
                return null;

            BooleanQuery query = new BooleanQuery();
            foreach (string word in words)
            {
                //Note: As for the WildcardQuery, we only add the wildcard to the end for performance reasons.
                query.Add(new FuzzyQuery(new Term(field, word)), Occur.SHOULD);
                query.Add(new WildcardQuery(new Term(field, word + "*")), Occur.SHOULD);
            }
            return query;
        }

    }

    public class NullFieldStrategy : FieldStrategy
    {
    }

    public class TermFieldStrategy : FieldStrategy
    {

        public override Query BuildQuery(string field, string value)
        {
            return new TermQuery(new Term(field, value));
        }

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
