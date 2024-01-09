﻿using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Web.Host.Providers.Index.Builder;
using DotJEM.Web.Host.Providers.Index.Schemas;
using Lucene.Net.QueryParsers.Classic;

namespace DotJEM.Web.Host.Providers.Index;

public interface IQueryParser
{
    Query BooleanQuery(IList<BooleanClause> clauses, bool disableCoord);
    Query Parse(string str);
}

public class CallContext
{
    private readonly Func<Query> func;

    public CallContext(Func<Query> func)
    {
        this.func = func;
    }

    public Query CallDefault()
    {
        return func();
    }
}

public interface IQueryParserConfiguration
{
    IFieldStrategy LookupStrategy(string field);
}

public class QueryParserConfiguration : IQueryParserConfiguration
{
    private readonly Dictionary<string, IFieldStrategy> strategies = new();

    public IFieldStrategy LookupStrategy(string field)
    {
        if(strategies.TryGetValue(field, out IFieldStrategy strategy))
            return strategy;

        return new FieldStrategy();
    }
}

public class MultiFieldQueryParser : QueryParser, IQueryParser
{
    private readonly string[] fields;
    private readonly string[] contentTypes;

    private readonly ISchemaCollection schemas;
    private readonly IQueryParserConfiguration parserConfig;

    public MultiFieldQueryParser(IJsonIndexConfiguration config, IQueryParserConfiguration parserConfig, ISchemaCollection schemas, params string[] fields)
        : base(config.Version, null, config.Analyzer)
    {
        this.parserConfig = parserConfig;
        this.fields = fields;
        this.schemas = schemas;
    }

    public Query BooleanQuery(IList<BooleanClause> clauses, bool disableCoord)
    {
        return GetBooleanQuery(clauses, disableCoord);
    }

    private IFieldQueryBuilder PrepareBuilderFor(string field)
    {
        JsonSchemaExtendedType type = schemas.ExtendedType(field);
        //TODO: Use "ForAll" strategy for now, we need to be able to extract possible contenttypes from the query and
        //      target their strategies. But this may turn into being very complex as different branches of a Query
        //      may target different 
        return parserConfig.LookupStrategy(field)
            .PrepareBuilder(this, field, type);

        //return index.Configuration.Field.Strategy(field)
        //    .PrepareBuilder(this, field, type);
    }

    protected override Query GetFieldQuery(string fieldName, string queryText, int slop)
    {
        if (fieldName != null)
        {
            var query = PrepareBuilderFor(fieldName)
                .BuildFieldQuery(new CallContext(() => base.GetFieldQuery(fieldName, queryText, false)), queryText, slop)
                .ApplySlop(slop);
            return query;
        }

        IList<BooleanClause> clauses = fields
            .Select(field => base.GetFieldQuery(field, queryText,false))
            .Where(field => field != null)
            .Select(query => query.ApplySlop(slop))
            .Select(query => new BooleanClause(query, Occur.SHOULD))
            .ToList();

        return clauses.Any() ? GetBooleanQuery(clauses, true) : null;
    }

    protected override Query GetFieldQuery(string field, string queryText, bool quoted)
    {
        return this.GetFieldQuery(field, queryText, 0);
    }


    protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
    {
        if (field != null)
        {
            var query = PrepareBuilderFor(field)
                .BuildFuzzyQuery(new CallContext(() => base.GetFuzzyQuery(field, termStr, minSimilarity)), termStr, minSimilarity);
            return query;
        }

        return GetBooleanQuery(fields
            .Select(t => new BooleanClause(GetFuzzyQuery(t, termStr, minSimilarity), Occur.SHOULD))
            .ToList(), true);
    }

    protected override Query GetPrefixQuery(string field, string termStr)
    {
        if (field != null)
        {
            var query = PrepareBuilderFor(field)
                .BuildPrefixQuery(new CallContext(() => base.GetPrefixQuery(field, termStr)), termStr);
            return query;
        }

        return GetBooleanQuery(fields
            .Select(t => new BooleanClause(GetPrefixQuery(t, termStr), Occur.SHOULD))
            .ToList(), true);
    }

    protected override Query GetWildcardQuery(string field, string termStr)
    {
        if (field != null)
        {
            var query = PrepareBuilderFor(field)
                .BuildWildcardQuery(new CallContext(() => base.GetWildcardQuery(field, termStr)), termStr);
            return query;
        }

        return GetBooleanQuery(fields
            .Select(t => new BooleanClause(GetWildcardQuery(t, termStr), Occur.SHOULD))
            .ToList(), true);
    }

    protected override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
    {
        part1 = (part1 == "*" ? "null" : part1);
        part2 = (part2 == "*" ? "null" : part2);

        if (field != null)
        {
            var query = PrepareBuilderFor(field)
                .BuildRangeQuery(new CallContext(() => base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive)), part1, part2, startInclusive, endInclusive);
            return query;
        }
        return GetBooleanQuery(fields
            .Select(t => new BooleanClause(GetRangeQuery(t, part1, part2, startInclusive, endInclusive), Occur.SHOULD))
            .ToList(), true);
    }

}

public static class QueryExtentions
{
    public static bool ApplySlop(this PhraseQuery query, int slop)
    {
        if (query != null) query.Slop = slop;
        return query != null;
    }

    public static bool ApplySlop(this MultiPhraseQuery query, int slop)
    {
        if (query != null) query.Slop = slop;
        return query != null;
    }

    // ReSharper disable UnusedMethodReturnValue.Local
    public static Query ApplySlop(this Query query, int slop)
    {
        if (ApplySlop(query as PhraseQuery, slop) || ApplySlop(query as MultiPhraseQuery, slop))
        {
        }
        return query;
    }
    // ReSharper restore UnusedMethodReturnValue.Local
}