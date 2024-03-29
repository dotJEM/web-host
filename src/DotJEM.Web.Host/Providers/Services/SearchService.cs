﻿ using System;
using System.Collections.Generic;
using System.Linq;
 using System.Threading.Tasks;
 using DotJEM.Diagnostic;
 using DotJEM.Json.Index;
using DotJEM.Json.Index.Searching;
 using DotJEM.Pipelines;
 using DotJEM.Web.Host.Providers.AsyncPipeline;
 using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

 namespace DotJEM.Web.Host.Providers.Services
{
    public sealed class SearchResult
    {
        public long TotalCount { get; set; }
        public IReadOnlyList<JObject> Results { get; set; }

        public SearchResult(ISearchResult result)
        {
            //Note: We must do the actual enumeration here to kick of the search, otherwise TotalCount is 0.
            Results = result.Select(hit => hit.Entity).ToArray();
            TotalCount = result.TotalCount;
        }

        public SearchResult(IEnumerable<JObject> results, long totalCount)
        {
            Results = results.ToArray();
            TotalCount = totalCount;
        }
    }

    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(string query, int skip = 0, int take = 20);

        SearchResult Search(dynamic query, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc");
    }

    public interface ISearchContext : IPipelineContext
    {
        string Query { get; }
        int Skip { get; }
        int Take { get; }
    }

    public class SearchContext : PipelineContext, ISearchContext
    {
        public string Query => (string)Get("query");
        public int Skip => (int)Get("skip");
        public int Take => (int)Get("take");

        public SearchContext(string query, int take, int skip)
        {
            Set("type", "SEARCH");
            Set(nameof(query), query);
            Set(nameof(take), take);
            Set(nameof(skip), skip);
        }
    }

    public class SearchService : ISearchService
    {
        private readonly IStorageIndex index;
        private readonly IPipelines pipelines;
        private readonly ILogger performance;
        private readonly IPipelineContextFactory contextFactory;

        public SearchService(IStorageIndex index, IPipelines pipelines, ILogger performance, IPipelineContextFactory contextFactory = null)
        {
            this.index = index;
            this.pipelines = pipelines;
            this.performance = performance;
            this.contextFactory = contextFactory ?? new DefaultPipelineContextFactory();
        }

        public async Task<SearchResult> SearchAsync(string query, int skip = 0, int take = 20)
        {

            ISearchContext context = contextFactory.CreateSearchContext(query, take, skip);
            ICompiledPipeline<SearchResult> pipeline = pipelines
                .For(context, async ctx =>
                {
                    ISearchResult result = index
                        .Search(query)
                        .Skip(skip)
                        .Take(take);

                    SearchResult resolved =  new (result);
                    await performance.LogAsync("search", new
                    {
                        totalTime = (long)result.TotalTime.TotalMilliseconds,
                        searchTime = (long)result.SearchTime.TotalMilliseconds,
                        loadTime = (long)result.LoadTime.TotalMilliseconds,
                        query,
                        skip,
                        take,
                        results = result.TotalCount
                    });
                    return resolved;
                });
            return await pipeline.Invoke();
            
            //TODO: Throw exception on invalid query.
            //if (string.IsNullOrWhiteSpace(query))
            //    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a query.");

            //ISearchResult result = index
            //    .Search(query)
            //    .Skip(skip)
            //    .Take(take);

            //TODO: extract contenttype based on configuration.
            //SearchResult searchResult = new SearchResult(result
            //    //TODO: need to introduce an alternative here.
            //    //.Select(hit => pipeline.ExecuteAfterGet(hit.Json, (string)hit.Json.contentType, pipeline.CreateContext((string)hit.Json.contentType, (JObject)hit.Json)))
            //    .ToArray(), result.TotalCount);

            //performance.LogAsync("search", new
            //{
            //    totalTime = (long)result.TotalTime.TotalMilliseconds,
            //    searchTime = (long)result.SearchTime.TotalMilliseconds,
            //    loadTime = (long)result.LoadTime.TotalMilliseconds,
            //    query, skip, take,
            //    results = result.TotalCount
            //});
            //return searchResult;
        }

        public SearchResult Search(dynamic query, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc")
        {
            JObject reduceObj = query as JObject ?? JObject.FromObject(query);

            ISearchResult result = index.Search(reduceObj).Skip(skip).Take(take);
            if (!string.IsNullOrEmpty(sort))
            {
                result.Sort(CreateSortObject(sort));
            }
            SearchResult searchResult = new SearchResult(result);
            return searchResult;
        }

        private Sort CreateSortObject(string sort)
        {
            return new Sort(sort.Split(',').Select(CreateSortField).ToArray());
        }

        private SortField CreateSortField(string sort)
        {
            //TODO: Sort by other types as well.
            string[] fields = sort.Split(':');
            return new SortField(fields[0], SortField.LONG, fields[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}