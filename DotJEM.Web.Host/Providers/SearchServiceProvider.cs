using DotJEM.Json.Index;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers
{
    public class SearchServiceProvider : ServiceProvider<ISearchService>
    {
        //TODO: Currently there is only one index, in the future we might wan't to make a mapping between areas and multiple indexes.
        //      or alternatively switch to a 1:1 strategy where we then just have to perform multiple searches if we wish to lookup across.
        public SearchServiceProvider(IStorageIndex index, IPipeline pipeline, IPerformanceLogger performance)
            : base(name => new SearchService(index, pipeline, performance))
        {
        }
    }
}