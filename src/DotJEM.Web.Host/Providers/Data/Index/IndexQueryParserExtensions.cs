using System;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Results;
using DotJEM.Json.Index2.Searching;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Data.Index;

public static class IndexQueryParserExtensions
{
    public static IJsonIndexBuilder WithClassicLuceneQueryParser(this IJsonIndexBuilder self, ISchemaCollection schemas,IQueryParserConfiguration config)
        => self
            .TryWithService(schemas)
            .TryWithService(config)
            .TryWithService<IQueryParserFactory>(x
                => new QueryParserFactory(x));

    public static ISearch Search(this IJsonIndexSearcher self, string query)
    {
        IQueryParser parser = self.Index.Configuration.ResolveParser();
        //MultiFieldQueryParserIntegration parser = new MultiFieldQueryParserIntegration();
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
        IQueryParserFactory factory = self.Get<IQueryParserFactory>() ?? throw new Exception("Query parser not configured.");
        return factory.Create();
    }

}

public interface IQueryParserFactory
{
    IQueryParser Create();
}

class QueryParserFactory : IQueryParserFactory
{
    private readonly IJsonIndexConfiguration config;

    public QueryParserFactory(IJsonIndexConfiguration config)
    {
        this.config = config;
    }

    public IQueryParser Create() 
        => new MultiFieldQueryParserIntegration(config, config.Get<IQueryParserConfiguration>(), config.Get<ISchemaCollection>());
}

