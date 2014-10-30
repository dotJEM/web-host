using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Controllers
{
    public class SearchResult
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

    public class SearchController : ApiController
    {
        private readonly IStorageIndex index;

        public SearchController(IStorageIndex index)
        {
            this.index = index;
        }

        [HttpGet]
        public dynamic Get([FromUri]string query, [FromUri]int skip = 0, [FromUri]int take = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
                Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a query.");

            ISearchResult result = index.Search(query).Skip(skip).Take(take);
            return new SearchResult(result);
        }

        [HttpPost]
        public dynamic Post([FromBody]dynamic value, [FromUri]string contentType = "", [FromUri]int skip = 0, [FromUri]int take = 25, [FromUri]string sort = "$created:desc")
        {
            ISearchResult result = index.Search((JObject)value).Skip(skip).Take(take).Sort(

                new Sort(sort.Split(',').Select(x =>
                {
                    string[] f = x.Split(':');
                    return new SortField(f[0], SortField.LONG, f[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase));
                }).ToArray())

                );

            return new SearchResult(result);
        }
    }
}