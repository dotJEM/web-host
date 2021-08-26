using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.Core;
using DotJEM.Diagnostic;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using DotJEM.Web.Host.Tasks;
using Newtonsoft.Json.Linq;
using IPipeline = DotJEM.Web.Host.Providers.Pipeline.IPipeline;

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

        public class HttpGetPipelineContext : IJsonPipelineContext
        {
            private readonly string method;
            private readonly string contentType;
            private readonly Guid id;
            public Guid Id => id;

            public HttpGetPipelineContext(string method, string contentType, Guid id)
            {
                this.method = method;
                this.contentType = contentType;
                this.id = id;
            }


            public bool TryGetValue(string key, out string value)
            {
                switch (key)
                {
                    case nameof(method): 
                        value = method;
                        return true;
                    case nameof(contentType):
                        value = method;
                        return true;
                    case nameof(id): 
                        value = id.ToString("D");
                        return true;
                }

                value = null;
                return false;
            }

            public object GetParameter(string key)
            {
                return key switch
                {
                    nameof(method) => method,
                    nameof(contentType) => contentType,
                    nameof(id) => id,
                    _ => null
                };
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                return this;
            }
        }

        public Task<JObject> GetAsync(Guid id, string contentType)
        {
            HttpGetPipelineContext context = new ("GET", contentType, id);
            ICompiledPipeline<HttpGetPipelineContext> pipeline = pipelines
                .For(context, async ctx => area.Get(ctx.Id));

            return pipeline.Invoke(context);
            //IGetContext context = pipeline.ContextFactory.CreateGetContext(contentType);
            //return pipeline.Get(id, context);
        }
        
        public class HttpPostPipelineContext : IJsonPipelineContext
        {
            private readonly string method;
            private readonly string contentType;
            private readonly JObject entity;

            public string ContentType => contentType;
            public JObject Entity => entity;

            public HttpPostPipelineContext(string method, string contentType, JObject entity)
            {
                this.method = method;
                this.contentType = contentType;
                this.entity = entity;
            }


            public bool TryGetValue(string key, out string value)
            {
                switch (key)
                {
                    case nameof(method): 
                        value = method;
                        return true;
                    case nameof(contentType):
                        value = method;
                        return true;
                }
                value = null;
                return false;
            }

            public object GetParameter(string key)
            {
                return key switch
                {
                    nameof(method) => method,
                    nameof(contentType) => contentType,
                    nameof(entity) => entity,
                    _ => null
                };
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                return this;
            }
        }

        public async Task<JObject> PostAsync(string contentType, JObject entity)
        {
            HttpPostPipelineContext context = new ("POST", contentType, entity);
            ICompiledPipeline<HttpPostPipelineContext> pipeline = pipelines
                .For(context, async ctx => area.Insert(ctx.ContentType, ctx.Entity));
            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }
        
        public class HttpPutPipelineContext : IJsonPipelineContext
        {
            private readonly string method;
            private readonly string contentType;
            private readonly JObject entity;
            private readonly JObject previous;
            private readonly Guid id;
            public Guid Id => id;

            public string ContentType => contentType;
            public JObject Entity => entity;

            public HttpPutPipelineContext(string method, string contentType, Guid id, JObject entity, JObject previous)
            {
                this.method = method;
                this.contentType = contentType;
                this.id = id;
                this.entity = entity;
                this.previous = previous;
            }


            public bool TryGetValue(string key, out string value)
            {
                switch (key)
                {
                    case nameof(method): 
                        value = method;
                        return true;
                    case nameof(contentType):
                        value = method;
                        return true;
                    case nameof(id):
                        value = id.ToString("D");
                        return true;
                }
                value = null;
                return false;
            }

            public object GetParameter(string key)
            {
                return key switch
                {
                    nameof(method) => method,
                    nameof(contentType) => contentType,
                    nameof(entity) => entity,
                    nameof(previous) => previous,
                    nameof(id) => id,
                    _ => null
                };
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                return this;
            }
        }

        public async Task<JObject> PutAsync(Guid id, string contentType, JObject entity)
        {
            JObject prev = area.Get(id);
            entity = merger.EnsureMerge(id, entity, prev);
            
            HttpPutPipelineContext context = new ("PUT", contentType, id, entity, prev);
            ICompiledPipeline<HttpPutPipelineContext> pipeline = pipelines
                .For(context, async ctx => area.Update(ctx.Id, ctx.Entity));
            //IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, prev);

            //entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);

            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }
        
        public class HttpPatchPipelineContext : IJsonPipelineContext
        {
            private readonly string method;
            private readonly string contentType;
            private readonly JObject entity;
            private readonly JObject previous;
            private readonly Guid id;
            public Guid Id => id;

            public string ContentType => contentType;
            public JObject Entity => entity;

            public HttpPatchPipelineContext(string method, string contentType, Guid id, JObject entity, JObject previous)
            {
                this.method = method;
                this.contentType = contentType;
                this.id = id;
                this.entity = entity;
                this.previous = previous;
            }

            public bool TryGetValue(string key, out string value)
            {
                switch (key)
                {
                    case nameof(method): 
                        value = method;
                        return true;
                    case nameof(contentType):
                        value = method;
                        return true;
                    case nameof(id):
                        value = id.ToString("D");
                        return true;
                }
                value = null;
                return false;
            }

            public object GetParameter(string key)
            {
                return key switch
                {
                    nameof(method) => method,
                    nameof(contentType) => contentType,
                    nameof(entity) => entity,
                    nameof(previous) => previous,
                    nameof(id) => id,
                    _ => null
                };
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                return this;
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
            
            HttpPatchPipelineContext context = new ("PATCH", contentType, id, entity, prev);
            ICompiledPipeline<HttpPatchPipelineContext> pipeline = pipelines
                .For(context, async ctx => area.Update(ctx.Id, ctx.Entity));
            
            //IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, prev);
            //entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);

            entity = await pipeline.Invoke(context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public class HttpDeletePipelineContext : IJsonPipelineContext
        {
            private string method;
            private string contentType;
            private JObject previous;
            private Guid id;
            public Guid Id => id;

            public string ContentType => contentType;
            public JObject Previous => previous;

            public HttpDeletePipelineContext(string method, string contentType, Guid id, JObject previous)
            {
                this.method = method;
                this.contentType = contentType;
                this.id = id;
                this.previous = previous;
            }

            public bool TryGetValue(string key, out string value)
            {
                switch (key)
                {
                    case nameof(method): 
                        value = method;
                        return true;
                    case nameof(contentType):
                        value = method;
                        return true;
                    case nameof(id):
                        value = id.ToString("D");
                        return true;
                }
                value = null;
                return false;
            }

            public object GetParameter(string key)
            {
                return key switch
                {
                    nameof(method) => method,
                    nameof(contentType) => contentType,
                    nameof(previous) => previous,
                    nameof(id) => id,
                    _ => null
                };
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                return this;
            }
        }


        public async Task<JObject> DeleteAsync(Guid id, string contentType)
        {
            JObject prev = area.Get(id);
            if (prev == null)
                return null;

            HttpDeletePipelineContext context = new ("DELETE", contentType, id, prev);
            ICompiledPipeline<HttpDeletePipelineContext> pipeline = pipelines
                .For(context, async ctx => area.Delete(ctx.Id));
            
            //IDeleteContext context = pipeline.ContextFactory.CreateDeleteContext(contentType, prev);

            //JObject deleted = await pipeline.Delete(id, context);
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

    public static class PipelineExt
    {
        public static PipelineContext CreateContext(this IPipeline self, string contentType, JObject json, [CallerMemberName] string caller = "")
        {
            return self.ContextFactory.Create(caller, contentType, json);
        }
    }
}