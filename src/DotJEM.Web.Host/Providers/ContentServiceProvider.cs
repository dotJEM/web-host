using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using DotJEM.Web.Host.Providers.AsyncPipeline.Handlers;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers
{
    public class ContentServiceProvider : ServiceProvider<IContentService>
    {
        private readonly IStorageContext context;

        public ContentServiceProvider(IStorageContext context, IStorageIndexManager manager, IAsyncPipelineFactory pipeline, IJsonMergeVisitor merger, ILogger logger, IPerformanceLogAspectSignatureCache cache = null)
            : base(name => new ContentService(context.Area(name), manager, pipeline.Create(new StorageArea(context.Area(name))), merger), logger, cache)
        {
            this.context = context;
        }

        public override bool Release(string areaName)
        {
            return base.Release(areaName) && context.Release(areaName);
        }
        public class StorageArea : IAsyncPipelineHandler
        {
            private readonly IStorageArea area;

            public StorageArea(IStorageArea area)
            {
                this.area = area;
            }

            public Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next) => Task.Run(() => area.Get(id));
            public Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next) => Task.Run(() =>area.Insert(context.ContentType, entity));
            public Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next) => Task.Run(() =>area.Update(id, entity));
            public Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next) => Task.Run(() =>area.Update(id, entity));
            public Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next) => Task.Run(() =>area.Delete(id));
        }
    }

}