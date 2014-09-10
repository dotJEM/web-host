using System;
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

            ILuceneSearcher searcher = index.CreateSearcher();
            ISearchResult result = searcher.Search(query);

            dynamic json = new JObject();
            json.Results = JArray.FromObject(result.Skip(skip).Take(take).Select(hit => hit.Json));
            json.TotalCount = result.TotalCount;
            return json;
        }

        [HttpPost]
        public dynamic Post([FromBody]dynamic value, [FromUri]string contentType = "", [FromUri]int skip = 0, [FromUri]int take = 25, [FromUri]string sort = "_Created:desc")
        {
            ILuceneSearcher searcher = index.CreateSearcher();
            ISearchResult result = searcher.Search((JObject)value, contentType).Skip(skip).Take(take).Sort(

                new Sort(sort.Split(',').Select(x =>
                {
                    string[] f = x.Split(':');
                    return new SortField(f[0], SortField.LONG, f[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase));
                }).ToArray())

                );

            dynamic json = new JObject();
            json.Results = JArray.FromObject(result.Select(hit => hit.Json));
            json.TotalCount = result.TotalCount;
            return json;
        }
    }
}