using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.Core;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IContentService
    {
        IStorageArea StorageArea { get; }
        Task<JObject> GetAsync(Guid id, string contentType);
        Task<JObject> PostAsync(string contentType, JObject entity);
        Task<JObject> PutAsync(Guid id, string contentType, JObject entity);
        Task<JObject> PatchAsync(Guid id, string contentType, JObject entity);
        Task<JObject> DeleteAsync(Guid id, string contentType);
    }

    //TODO: Apply Pipeline for all requests.
    [Interceptor(typeof(PerformanceLogAspect))]
    public class ContentService : IContentService
    {
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IPipelines pipelines;
        private readonly IContentMergeService merger;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageArea area,
            IStorageIndexManager manager,
            IPipelines pipelines, 
            IJsonMergeVisitor merger)
        {
            this.area = area;
            this.manager = manager;
            this.pipelines = pipelines;
            this.merger = new ContentMergeService(merger, area);
        }

        public class HttpPipelineContext : PipelineContext
        {
            public HttpPipelineContext(string method, string contentType)
            {
                this.Set(nameof(method), method);
                this.Set(nameof(contentType), contentType);
            }
        }

        public class HttpGetContext : HttpPipelineContext
        {
            public string ContentType => (string)GetParameter("contentType");
            public Guid Id => (Guid) GetParameter("id");

            public HttpGetContext(string contentType, Guid id)
                : base("GET", contentType)
            {
                Set(nameof(id), id);
            }
        }

        public Task<JObject> GetAsync(Guid id, string contentType)
        {
            HttpGetContext context = new (contentType, id);
            ICompiledPipeline<HttpGetContext, JObject> pipeline = pipelines
                .For(context, async ctx => area.Get(ctx.Id));

            return pipeline.Invoke(context);
        }

        public class HttpPostContext : HttpPipelineContext
        {
            public string ContentType => (string)GetParameter("contentType");
            public JObject Entity => (JObject)GetParameter("entity");

            public HttpPostContext( string contentType, JObject entity)
                : base("POST", contentType)
            {
                Set(nameof(entity), entity);
            }
        }

        public async Task<JObject> PostAsync(string contentType, JObject entity)
        {
            HttpPostContext context = new (contentType, entity);
            ICompiledPipeline<HttpPostContext, JObject> pipeline = pipelines
                .For(context, async ctx => area.Insert(ctx.ContentType, ctx.Entity));
            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public class HttpPutContext : HttpPipelineContext
        {
            public string ContentType => (string)GetParameter("contentType");
            public Guid Id => (Guid) GetParameter("id");
            public JObject Entity => (JObject)GetParameter("entity");
            public JObject Previous => (JObject)GetParameter("previous");

            public HttpPutContext(string contentType, Guid id, JObject entity, JObject previous)
                : base("PUT", contentType)
            {
                Set(nameof(id), id);
                Set(nameof(entity), entity);
                Set(nameof(previous), previous);
            }
        }


        public async Task<JObject> PutAsync(Guid id, string contentType, JObject entity)
        {
            JObject prev = area.Get(id);
            entity = merger.EnsureMerge(id, entity, prev);

            HttpPutContext context = new (contentType, id, entity, prev);
            ICompiledPipeline<HttpPutContext, JObject> pipeline = pipelines
                .For(context, async ctx => area.Update(ctx.Id, ctx.Entity));

            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public class HttpPatchContext : HttpPipelineContext
        {
            public string ContentType => (string)GetParameter("contentType");
            public Guid Id => (Guid)GetParameter("id");
            public JObject Entity => (JObject)GetParameter("entity");
            public JObject Previous => (JObject)GetParameter("previous");

            public HttpPatchContext(string contentType, Guid id, JObject entity, JObject previous)
                : base("PATCH", contentType)
            {
                Set(nameof(id), id);
                Set(nameof(entity), entity);
                Set(nameof(previous), previous);
            }
        }

        public async Task<JObject> PatchAsync(Guid id, string contentType, JObject entity)
        {
            JObject prev = area.Get(id);
            //TODO: This can be done better by simply merging the prev into the entity but skipping
            //      values that are present in the entity. However, we might wan't to inclide the raw patch
            //      content in the pipeline as well, so we need to consider pro/cons
            JObject clone = (JObject)prev.DeepClone();
            clone.Merge(entity);
            entity = clone;
            entity = merger.EnsureMerge(id, entity, prev);

            HttpPatchContext context = new (contentType, id, entity, prev);
            ICompiledPipeline<HttpPatchContext, JObject> pipeline = pipelines
                .For(context, async ctx => area.Update(ctx.Id, ctx.Entity));
            
            //IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, prev);
            //entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);

            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public class HttpDeleteContext : HttpPipelineContext
        {
            public string ContentType => (string)GetParameter("contentType");
            public Guid Id => (Guid)GetParameter("id");

            public HttpDeleteContext(string contentType, Guid id, JObject previous)
                : base("DELETE", contentType)
            {
                Set(nameof(id), id);
                Set(nameof(previous), previous);
            }
        }

        public async Task<JObject> DeleteAsync(Guid id, string contentType)
        {
            JObject prev = area.Get(id);
            if (prev == null)
                return null;

            HttpDeleteContext context = new (contentType, id, prev);
            ICompiledPipeline<HttpDeleteContext, JObject> pipeline = pipelines
                .For(context, async ctx => area.Delete(ctx.Id));
            
            JObject deleted = await pipeline.Invoke(context);
            
            //Note: This may pose a bit of a problem, because we don't lock so far out (performance),
            //      this can theoretically happen if two threads or two nodes are trying to delete the
            //      same object at the same time.
            if (deleted == null)
                return null;

            manager.QueueDelete(deleted);
            return deleted;
        }
    }



    //public static class PipelineExt
    //{
    //    public static PipelineContext CreateContext(this IPipeline self, string contentType, JObject json, [CallerMemberName] string caller = "")
    //    {
    //        return self.ContextFactory.Create(caller, contentType, json);
    //    }
    //}
}