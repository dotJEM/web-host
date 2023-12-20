using System;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Results;
using DotJEM.Json.Index2.Searching;
using Lucene.Net.Search;

namespace DotJEM.Web.Host.Providers.Index;

public static class IndexQueryParserExtensions
{
    public static IJsonIndexBuilder WithSimplifiedLuceneQueryParser(this IJsonIndexBuilder self)
        => self.TryWithService<IQueryParser>(config => new MultiFieldQueryParser(config.FieldInformationManager, config.Analyzer));

    public static ISearch Search(this IJsonIndexSearcher self, string query)
    {
        IQueryParser parser = self.Index.Configuration.ResolveParser();
        //MultiFieldQueryParser parser = new MultiFieldQueryParser();


        //LuceneQueryInfo queryInfo = parser.Parse(query);
        return self.Search(queryInfo.Query).OrderBy(queryInfo.Sort);
    }

    public static ISearch Search(this IJsonIndex self, string query)
    {
        IQueryParser parser = self.Configuration.ResolveParser();
        LuceneQueryInfo queryInfo = parser.Parse(query);
        return self.CreateSearcher().Search(queryInfo.Query).OrderBy(queryInfo.Sort);
    }

    private static IQueryParser ResolveParser(this IJsonIndexConfiguration self)
    {
        return self.Get<IQueryParser>() ?? throw new Exception("Query parser not configured.");
    }

}