using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers
{
    public class HistoryServiceProvider : ServiceProvider<IHistoryService>
    {
        private readonly IStorageContext context;

        public HistoryServiceProvider(IStorageContext context, IStorageIndexManager manager, IAsyncPipeline pipeline, ILogger performance)
            : base(name => new HistoryService(context.Area(name), manager, pipeline, performance))
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
    }

    public class ContentServiceProvider : ServiceProvider<IContentService>
    {
        private readonly IStorageContext context;

        public ContentServiceProvider(IStorageContext context, IStorageIndexManager manager, IAsyncPipelineFactory pipeline, IJsonMergeVisitor merger)
            : base(name => new ContentService(context.Area(name), manager, pipeline.Create(new AsyncPipelineHandlerTermination(context.Area(name))), merger))
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
    }

    public class AsyncPipelineHandlerTermination : IAsyncPipelineHandler
    {
        private readonly IStorageArea area;

        public AsyncPipelineHandlerTermination(IStorageArea area)
        {
            this.area = area;
        }

        public async Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next) => area.Get(id);
        public async Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next) => area.Insert(context.ContentType, entity);
        public async Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next) => area.Update(id, entity);
        public async Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next) => area.Update(id, entity);
        public async Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next) => area.Delete(id);
    }
}