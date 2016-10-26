using System.Web.Http;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Searching;
using DotJEM.Web.Host;
using Lucene.Net.QueryParsers;

namespace Demo.Server
{
    public class DemoSearchController : WebHostApiController
    {
        private readonly IStorageIndex index;

        public DemoSearchController(IStorageIndex index)
        {
            this.index = index;
        }

        [HttpGet]
        public dynamic Get([FromUri]string query, [FromUri]int skip = 0, [FromUri]int take = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Must specify a query.");
            }

            try
            {
                ISearchResult result = index
                    .Search(query)
                    .Skip(skip)
                    .Take(take);
                return new LeoSearchResult(result, take, skip);
            }
            catch (ParseException ex)
            {
                return BadRequest("Query is invalid: " + ex.Message);
            }

        }

    }
}