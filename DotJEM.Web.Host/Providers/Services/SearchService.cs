 using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public sealed class SearchResult
    {
        public long TotalCount { get; set; }
        public IEnumerable<dynamic> Results { get; set; }

        public SearchResult(ISearchResult result)
        {
            //Note: We must do the actual enumeration here to kick of the search, otherwise TotalCount is 0.
            Results = result.Select(hit => hit.Json).ToArray();
            TotalCount = result.TotalCount;
        }
    }

    public interface ISearchService
    {
        SearchResult Search(string query, int skip = 0, int take = 20);
        SearchResult Reduce(dynamic reduce, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc");

        //// SEARCH CONTROLLER
        //[HttpGet]
        //public dynamic Get([FromUri]string query, [FromUri]int skip = 0, [FromUri]int take = 25)
        //{
        //    if (string.IsNullOrWhiteSpace(query))
        //        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a query.");

        //    ILuceneSearcher searcher = index.CreateSearcher();
        //    ISearchResult result = searcher.Search(query).Skip(skip).Take(take);
        //    return new SearchResult(result);
        //}

        //[HttpPost]
        //public dynamic Post([FromBody]dynamic value, [FromUri]string contentType = "", [FromUri]int skip = 0, [FromUri]int take = 25, [FromUri]string sort = "$created:desc")
        //{
        //    ILuceneSearcher searcher = index.CreateSearcher();
        //    ISearchResult result = searcher.Search((JObject)value, contentType).Skip(skip).Take(take).Sort(

        //        new Sort(sort.Split(',').Select(x =>
        //        {
        //            string[] f = x.Split(':');
        //            return new SortField(f[0], SortField.LONG, f[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase));
        //        }).ToArray())

        //        );

        //    return new SearchResult(result);
        //}
    }
    public class SearchService : ISearchService
    {
        private readonly IStorageIndex index;

        public SearchService(IStorageIndex index)
        {
            this.index = index;
        }

        public SearchResult Search(string query, int skip = 0, int take = 20)
        {
            //TODO: Throw exception on invalid query.
            //if (string.IsNullOrWhiteSpace(query))
            //    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a query.");

            ISearchResult result = index.Search(query).Skip(skip).Take(take);
            return new SearchResult(result);
        }

        public SearchResult Reduce(dynamic reduce, string contentType = null, int skip = 0, int take = 20, string sort = "$created:desc")
        {
            JObject reduceObj = reduce as JObject ?? JObject.FromObject(reduce);

            ISearchResult result = index.Search(reduceObj).Skip(skip).Take(take);
            if (!string.IsNullOrEmpty(sort))
            {
                result.Sort(CreateSortObject(sort));
            }
            return new SearchResult(result);
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