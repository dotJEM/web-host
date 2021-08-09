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
        Task<JObject> DeleteAsync(Guid id, string contentType);
    }

    //TODO: Apply Pipeline for all requests.
    [Interceptor(typeof(PerformanceLogAspect))]
    public class ContentService : IContentService
    {
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IAsyncPipeline pipeline;
        private readonly IContentMergeService merger;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageArea area, IStorageIndexManager manager, IAsyncPipeline pipeline, IJsonMergeVisitor merger)
        {
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
            this.merger = new ContentMergeService(merger, area);
        }

        public Task<JObject> GetAsync(Guid id, string contentType)
        {
            IGetContext context = pipeline.ContextFactory.CreateGetContext(contentType);
            return pipeline.Get(id, context);
        }

        public async Task<JObject> PostAsync(string contentType, JObject entity)
        {
            IPostContext context = pipeline.ContextFactory.CreatePostContext(contentType);

            entity = await pipeline.Post(entity, context).ConfigureAwait(false);
            manager.QueueUpdate(entity);
            return entity;
        }

        public async Task<JObject> PutAsync(Guid id, string contentType, JObject entity)
        {
            JObject prev = area.Get(id);
            IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, prev);
            
            entity = merger.EnsureMerge(id, entity, prev);
            entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);
            manager.QueueUpdate(entity);
            return entity;
        }

        public async Task<JObject> DeleteAsync(Guid id, string contentType)
        {
            JObject prev = area.Get(id);
            IDeleteContext context = pipeline.ContextFactory.CreateDeleteContext(contentType, prev);

            JObject deleted = await pipeline.Delete(id, context);
            if (deleted == null)
                return null;

            manager.QueueDelete(deleted);
            return deleted;
        }
    }

    public interface IHistoryService
    {
        IStorageArea StorageArea { get; }

        JObject History(Guid id, string contentType, int version);
        JObject Revert(Guid id, string contentType, int version);

        IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        IEnumerable<JObject> Deleted(string contentType, DateTime? from = null, DateTime? to = null);
    }

    [Interceptor(typeof(PerformanceLogAspect))]
    public class HistoryService : IHistoryService
    {
        private const string TRACK_TYPE = "content";

        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IAsyncPipeline pipeline;
        private readonly ILogger performance;

        public IStorageArea StorageArea => area;

        public HistoryService(IStorageArea area, IStorageIndexManager manager, IAsyncPipeline pipeline, ILogger performance)
        {
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
            this.performance = performance;
        }

        public JObject History(Guid id, string contentType, int version)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return null;

            return area.History.Get(id, version);
        }

        public IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Enumerable.Empty<JObject>();

            return area.History.Get(id, from, to);
        }

        public IEnumerable<JObject> Deleted(string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Enumerable.Empty<JObject>();

            return area.History.GetDeleted(contentType, from, to);
        }

        public JObject Revert(Guid id, string contentType, int version)
        {
            if (!area.HistoryEnabled)
                throw new InvalidOperationException("Cannot revert document when history is not enabled.");

            return performance.TrackFunction(() =>
            {
                JObject current = area.Get(id);
                //JObject prev = area.Get(id);
                IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, current);
                JObject entity = area.History.Get(id, version);
                area.Update(id, entity);
                manager.QueueUpdate(entity);
                return entity;
                    //entity = merger.EnsureMerge(id, entity, prev);
                //entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);
                //manager.QueueUpdate(entity);
                //return entity;

                //using (PipelineContext context = pipeline.CreateContext(contentType, current))
                //{
                //    JObject entity = area.History.Get(id, version);
                //    entity = pipeline.ExecuteBeforeRevert(entity, current, contentType, context);
                //    area.Update(id, entity);
                //    entity = pipeline.ExecuteAfterRevert(entity, current, contentType, context);
                //    manager.QueueUpdate(entity);
                //    return entity;
                //}
            }, TRACK_TYPE, new { fn =$"ContentService.Revert({id}, {contentType}, {version})"});
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