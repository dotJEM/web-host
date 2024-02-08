using DotJEM.Web.Host;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Web.Http;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Results;
using DotJEM.Web.Host.Providers.Data.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;

namespace Demo.Controllers
{
    public class GenericSearchResult
    {
        public long Take { get; set; }
        public long Skip { get; set; }
        public long Count { get; set; }

        public IEnumerable<object> Results { get; set; }

        public GenericSearchResult(SearchResults result, long take, long skip)
        {
            Take = take;
            Skip = skip;
            Results = result.Select(x=>x.Data).ToArray();
            Count = result.TotalHits;
        }
    }

    public class SearchController : WebHostApiController
    {
        private readonly IJsonIndex index;

        public SearchController(IJsonIndex index)
        {
            this.index = index;
        }

        [HttpGet]
        public dynamic Get([FromUri] string query, [FromUri] int skip = 0, [FromUri] int take = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Must specify a query.");
            }

            try
            {
                QueryInfo info = Build(query);
                SearchResults result = index
                    .Search(info.Query)
                    .OrderBy(info.Sort)
                    .Skip(skip)
                    .Take(take)
                    .Execute();
                return new GenericSearchResult(result, take, skip);
            }
            catch (ParseException ex)
            {
                return BadRequest("Query is invalid: " + ex.Message);
            }

        }

        private QueryInfo Build(string query)
        {
            string[] parts = query.Split(new[] { "ORDER BY" }, StringSplitOptions.None).Select(TrimString).ToArray();

            switch (parts.Length)
            {
                case 1:
                    return new QueryInfo { Query = query, Sort = DefaultSort };

                case 2:
                    return new QueryInfo { Query = parts[0], Sort = ParseSort(parts[1]) };

                default:
                    throw new ParseException("Query may only contain one ORDER BY block.");
            }
        }

        private Sort ParseSort(string sorting)
        {
            return new Sort(
                sorting.Split(',', ';').Select(TrimString).Select(field =>
                {
                    string[] parts = field.Split(':');
                    return CreateSortField(parts[0], parts.Length > 1 && parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase));
                }).ToArray());
        }

        private SortField CreateSortField(string name, bool reverse)
        {
            if (name.EndsWith(".@ticks"))
                return new SortField(name, SortFieldType.INT64, reverse);

            if (name == "certainty" || name == "importance" || name == "relevance")
                return new SortField(name, SortFieldType.INT64, reverse);

            if (name.EndsWith("length") || name.EndsWith("width") || name.EndsWith("grosston") || name.EndsWith("built") || name.EndsWith("height"))
                return new SortField(name, SortFieldType.INT64, reverse);

            return new SortField(name, SortFieldType.STRING, reverse);
        }

        public Sort DefaultSort => new Sort(new SortField("$created.@ticks", SortFieldType.INT64));

        private static string TrimString(string arg)
        {
            return arg.Trim();
        }
    }

    public class QueryInfo
    {
        public string Query { get; set; }
        public Sort Sort { get; set; }
    }
}