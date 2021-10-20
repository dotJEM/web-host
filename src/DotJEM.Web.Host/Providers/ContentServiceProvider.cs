using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Pipelines;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers
{
    public class ContentServiceProvider : ServiceProvider<IContentService>
    {
        private readonly IStorageContext context;

        public ContentServiceProvider(IStorageContext context, IStorageIndexManager manager, IPipelines pipelines, IJsonMergeVisitor merger, ILogger logger, IPerformanceLogAspectSignatureCache cache = null)
            : base(name => new ContentService(context.Area(name), manager, pipelines, merger), logger, cache)
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
    }

}