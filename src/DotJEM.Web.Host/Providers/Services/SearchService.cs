using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Diagnostic;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Results;
using DotJEM.Json.Index2.Searching;
using DotJEM.Web.Host.Providers.Pipeline;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services; 

 public sealed class JsonSearchResult
{
     public long TotalCount { get;  }
     public IReadOnlyCollection<dynamic> Results { get; }


     public JsonSearchResult(IReadOnlyCollection<JObject> results, long totalCount)
     {
         Results = results;
         TotalCount = totalCount;
     }
 }

 public interface ISearchService
 {
     JsonSearchResult Search(string query, int skip = 0, int take = 20);
     JsonSearchResult Search(dynamic query, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc");
 }

 public class SearchService : ISearchService
 {
     private readonly IJsonIndex index;
     private readonly IPipeline pipeline;
     private readonly ILogger performance;

     public SearchService(IJsonIndex index, IPipeline pipeline, ILogger performance)
     {
         this.index = index;
         this.pipeline = pipeline;
         this.performance = performance;
     }

     public JsonSearchResult Search(string query, int skip = 0, int take = 20)
     {
         //TODO: Throw exception on invalid query.
         //if (string.IsNullOrWhiteSpace(query))
         //    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a query.");


         ISearch search = index
             .Search(query)
             .Skip(skip)
             .Take(take);

         SearchResults result = search
             .Execute();

        JObject[] items = result
             .Select(result => pipeline.ExecuteAfterGet(result.Data,
                 (string)result.Data["contentType"], 
                 pipeline.CreateContext((string)result.Data["contentType"], result.Data)))
             .ToArray();

        JsonSearchResult searchResult = new JsonSearchResult(
            items,
            result.TotalHits
        );

        //TODO: extract contenttype based on configuration.
        //JsonSearchResult searchResult = new SearchResult(result
        //     .Select(hit => pipeline.ExecuteAfterGet(hit.Json, (string)hit.Json.contentType, pipeline.CreateContext((string)hit.Json.contentType, (JObject)hit.Json)))
        //     .ToArray(), result.TotalCount);

         performance.LogAsync("search", new
         {
             //totalTime = (long)result.TotalTime.TotalMilliseconds,
             //searchTime = (long)result.SearchTime.TotalMilliseconds,
             //loadTime = (long)result.LoadTime.TotalMilliseconds,
             query, skip, take,
             results = searchResult.TotalCount
         });
         return searchResult;
     }

     public JsonSearchResult Search(dynamic query, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc")
     {
         //JObject reduceObj = query as JObject ?? JObject.FromObject(query);
         //ISearch result = index.Search(reduceObj).Skip(skip).Take(take);
         //if (!string.IsNullOrEmpty(sort))
         //{
         //    result.Sort(CreateSortObject(sort));
         //}
         //SearchResult searchResult = new SearchResult(result);
         //return searchResult;

         throw new NotImplementedException();
     }

     private Sort CreateSortObject(string sort)
     {
         return new Sort(sort.Split(',').Select(CreateSortField).ToArray());
     }

     private SortField CreateSortField(string sort)
     {
         //TODO: Sort by other types as well.
         string[] fields = sort.Split(':');
         return new SortField(fields[0], SortFieldType.INT64, fields[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase));
     }
 }