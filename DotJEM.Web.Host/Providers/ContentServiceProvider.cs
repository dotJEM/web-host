using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers
{
    public class ContentServiceProvider : ServiceProvider<IContentService>
    {
        private readonly IStorageContext context;

        public ContentServiceProvider(IStorageIndex index, IStorageContext context, IStorageIndexManager manager) 
            : base(name => new ContentService(index, context.Area(name), manager))
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
    }
}