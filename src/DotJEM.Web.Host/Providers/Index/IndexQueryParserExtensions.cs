using System;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Results;
using DotJEM.Json.Index2.Searching;
using DotJEM.Web.Host.Providers.Index.Schemas;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Index;

public static class IndexQueryParserExtensions
{
    public static IJsonIndexBuilder WithClassicLuceneQueryParser(this IJsonIndexBuilder self, ISchemaCollection schemas,IQueryParserConfiguration config)
        => self
            .TryWithService(schemas)
            .TryWithService(config)
            .TryWithService<IQueryParser>(x
                => new MultiFieldQueryParser(x, x.Get<IQueryParserConfiguration>(), x.Get<ISchemaCollection>()));

    public static ISearch Search(this IJsonIndexSearcher self, string query)
    {
        IQueryParser parser = self.Index.Configuration.ResolveParser();
        //MultiFieldQueryParser parser = new MultiFieldQueryParser();
        Query queryInfo = parser.Parse(query);
        //LuceneQueryInfo queryInfo = parser.Parse(query);
        return self.Search(queryInfo);
    }

    public static ISearch Search(this IJsonIndex self, string query)
    {
        return self.CreateSearcher().Search(query);
    }

    private static IQueryParser ResolveParser(this IJsonIndexConfiguration self)
    {
        return self.Get<IQueryParser>() ?? throw new Exception("Query parser not configured.");
    }

}