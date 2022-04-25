using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.Core;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Pipelines;
using DotJEM.Web.Host.Diagnostics.Performance;
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

    public interface IPipelineContextFactory
    {
        IHttpGetContext CrateGetContext(string contentType, Guid id);
        IHttpPostContext CreatePostContext(string contentType, JObject entity);
        IHttpPutContext CreatePutContext(string contentType, Guid id, JObject entity, JObject prev);
        IHttpPatchContext CreatePatchContext(string contentType, Guid id, JObject entity, JObject prev);
        IHttpDeleteContext CreateDeleteContext(string contentType, Guid id, JObject prev);
        IRevertContext CrateRevertContext(string contentType, Guid id, int version, JObject target, JObject current);
        ISearchContext CreateSearchContext(string query, int take, int skip);
    }

    public class DefaultPipelineContextFactory : IPipelineContextFactory
    {
        public IHttpGetContext CrateGetContext(string contentType, Guid id) => new HttpGetContext(contentType, id);
        public IHttpPostContext CreatePostContext(string contentType, JObject entity) => new HttpPostContext(contentType, entity);
        public IHttpPutContext CreatePutContext(string contentType, Guid id, JObject entity, JObject prev) => new HttpPutContext(contentType, id, entity, prev);
        public IHttpPatchContext CreatePatchContext(string contentType, Guid id, JObject entity, JObject prev) => new HttpPatchContext(contentType, id, entity, prev);
        public IHttpDeleteContext CreateDeleteContext(string contentType, Guid id, JObject prev) => new HttpDeleteContext(contentType, id, prev);
        public IRevertContext CrateRevertContext(string contentType, Guid id, int version, JObject target, JObject current) => new RevertContext(contentType, id, version, target, current);
        public ISearchContext CreateSearchContext(string query, int take, int skip) => new SearchContext(query, take, skip);
    }


    //TODO: Apply Pipeline for all requests.
    [Interceptor(typeof(PerformanceLogAspect))]
    public class ContentService : IContentService
    {
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IPipelines pipelines;
        private readonly IPipelineContextFactory contextFactory;
        private readonly IContentMergeService merger;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageArea area, IStorageIndexManager manager, IPipelines pipelines, IJsonMergeVisitor merger, IPipelineContextFactory contextFactory = null)
        {
            this.area = area;
            this.manager = manager;
            this.pipelines = pipelines;
            this.contextFactory = contextFactory ?? new DefaultPipelineContextFactory();
            this.merger = new ContentMergeService(merger, area);
        }

        public Task<JObject> GetAsync(Guid id, string contentType)
        {
            IHttpGetContext context = contextFactory.CrateGetContext(contentType, id);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, ctx => Task.Run(() => area.Get(ctx.Id)));

            return pipeline.Invoke();
        }

        public async Task<JObject> PostAsync(string contentType, JObject entity)
        {
            //HttpPostContext context = new(contentType, entity);
            IHttpPostContext context = contextFactory.CreatePostContext(contentType, entity);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, ctx => Task.Run(() => area.Insert(ctx.ContentType, ctx.Entity)));
            entity = await pipeline.Invoke().ConfigureAwait(false);
            manager.QueueUpdate(entity);
            return entity;
        }
        public async Task<JObject> PutAsync(Guid id, string contentType, JObject entity)
        {
            JObject prev = area.Get(id);
            entity = merger.EnsureMerge(id, entity, prev);

            //HttpPutContext context = new(contentType, id, entity, prev);
            IHttpPutContext context = contextFactory.CreatePutContext(contentType, id, entity, prev);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, ctx => Task.Run(() => area.Update(ctx.Id, ctx.Entity)));

            entity = await pipeline.Invoke().ConfigureAwait(false);
            manager.QueueUpdate(entity);
            return entity;
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

            //HttpPatchContext context = new(contentType, id, entity, prev);
            IHttpPatchContext context = contextFactory.CreatePatchContext(contentType, id, entity, prev);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, ctx => Task.Run(() => area.Update(ctx.Id, ctx.Entity)));

            entity = await pipeline.Invoke().ConfigureAwait(false);
            manager.QueueUpdate(entity);
            return entity;
        }

        public async Task<JObject> DeleteAsync(Guid id, string contentType)
        {
            JObject prev = area.Get(id);
            if (prev == null)
                return null;

            IHttpDeleteContext context = contextFactory.CreateDeleteContext(contentType, id, prev);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, ctx => Task.Run(() => area.Delete(ctx.Id)));

            JObject deleted = await pipeline.Invoke().ConfigureAwait(false);

            //Note: This may pose a bit of a problem, because we don't lock so far out (performance),
            //      this can theoretically happen if two threads or two nodes are trying to delete the
            //      same object at the same time.
            if (deleted == null)
                return null;

            manager.QueueDelete(deleted);
            return deleted;
        }
    }

    public class HttpPipelineContext : PipelineContext
    {
        public HttpPipelineContext(string method, string contentType)
        {
            this.Set(nameof(method), method);
            this.Set(nameof(contentType), contentType);
        }
    }

    public interface IHttpGetContext:IPipelineContext
    {
        string ContentType { get; }
        Guid Id { get; }
    }

    public class HttpGetContext : HttpPipelineContext, IHttpGetContext
    {
        public string ContentType => (string)Get("contentType");
        public Guid Id => (Guid)Get("id");

        public HttpGetContext(string contentType, Guid id)
            : base("GET", contentType)
        {
            Set(nameof(id), id);
        }
    }

    public interface IHttpPostContext:IPipelineContext
    {
        string ContentType { get; }
        JObject Entity { get; }
    }

    public class HttpPostContext : HttpPipelineContext, IHttpPostContext
    {
        public string ContentType => (string)Get("contentType");
        public JObject Entity => (JObject)Get("entity");

        public HttpPostContext(string contentType, JObject entity)
            : base("POST", contentType)
        {
            Set(nameof(entity), entity);
        }
    }

    public interface IHttpPutContext:IPipelineContext
    {
        string ContentType { get; }
        Guid Id { get; }
        JObject Entity { get; }
        JObject Previous { get; }
    }

    public class HttpPutContext : HttpPipelineContext, IHttpPutContext
    {
        public string ContentType => (string)Get("contentType");
        public Guid Id => (Guid)Get("id");
        public JObject Entity => (JObject)Get("entity");
        public JObject Previous => (JObject)Get("previous");

        public HttpPutContext(string contentType, Guid id, JObject entity, JObject previous)
            : base("PUT", contentType)
        {
            Set(nameof(id), id);
            Set(nameof(entity), entity);
            Set(nameof(previous), previous);
        }
    }

    public interface IHttpPatchContext:IPipelineContext
    {
        string ContentType { get; }
        Guid Id { get; }
        JObject Entity { get; }
        JObject Previous { get; }
    }

    public class HttpPatchContext : HttpPipelineContext, IHttpPatchContext
    {
        public string ContentType => (string)Get("contentType");
        public Guid Id => (Guid)Get("id");
        public JObject Entity => (JObject)Get("entity");
        public JObject Previous => (JObject)Get("previous");

        public HttpPatchContext(string contentType, Guid id, JObject entity, JObject previous)
            : base("PATCH", contentType)
        {
            Set(nameof(id), id);
            Set(nameof(entity), entity);
            Set(nameof(previous), previous);
        }
    }

    public interface IHttpDeleteContext:IPipelineContext
    {
        string ContentType { get; }
        Guid Id { get; }
    }

    public class HttpDeleteContext : HttpPipelineContext, IHttpDeleteContext
    {
        public string ContentType => (string)Get("contentType");
        public Guid Id => (Guid)Get("id");

        public HttpDeleteContext(string contentType, Guid id, JObject previous)
            : base("DELETE", contentType)
        {
            Set(nameof(id), id);
            Set(nameof(previous), previous);
        }
    }
}