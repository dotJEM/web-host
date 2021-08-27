using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IHistoryService
    {
        IStorageArea StorageArea { get; }

        Task<JObject> History(Guid id, string contentType, int version);
        Task<JObject> Revert(Guid id, string contentType, int version);

        Task<IEnumerable<JObject>> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<JObject>> Deleted(string contentType, DateTime? from = null, DateTime? to = null);
    }

    public class HistoryService : IHistoryService
    {
        private readonly IPipelines pipelines;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;

        public IStorageArea StorageArea => area;

        public HistoryService(IStorageArea area, IStorageIndexManager manager, IPipelines pipelines)
        {
            this.area = area;
            this.manager = manager;
            this.pipelines = pipelines;
        }

        public Task<JObject> History(Guid id, string contentType, int version)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult((JObject)null);

            return Task.Run(() => area.History.Get(id, version));
        }

        public Task<IEnumerable<JObject>> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult(Enumerable.Empty<JObject>());

            return Task.Run(() => area.History.Get(id, from, to));
        }

        public Task<IEnumerable<JObject>> Deleted(string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult(Enumerable.Empty<JObject>());

            return Task.Run(() => area.History.GetDeleted(contentType, from, to));
        }

        public async Task<JObject> Revert(Guid id, string contentType, int version)
        {
            if (!area.HistoryEnabled)
                throw new InvalidOperationException("Cannot revert document when history is not enabled.");

            
            JObject current = area.Get(id);
            JObject target = area.History.Get(id, version);

            RevertPipelineContext context = new RevertPipelineContext(id, contentType, version, target, current);
            var pipeline = pipelines.For(context, async ctx => area.Update(ctx.Id, context.Target));

            JObject result = await pipeline.Invoke(context);
            manager.QueueUpdate(result);
            return result;

            //return pipeline.Execute(contentType, version, target, current, context =>
            //{
            //    JObject result = area.Update(id, context.Target);
            //    manager.QueueUpdate(result);
            //    return result;
            //});
            throw new NotImplementedException();
            /**
             * 
             * IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, current);
             * JObject entity = area.History.Get(id, version);
             * area.Update(id, entity);
             * manager.QueueUpdate(entity);
             * return entity;
             * 
             * entity = merger.EnsureMerge(id, entity, prev);
             * entity = await pipeline.Put(id, entity, context).ConfigureAwait(false);
             * manager.QueueUpdate(entity);
             * return entity;
             * 
             * using (PipelineContext context = pipeline.CreateContext(contentType, current))
             * {
             *     JObject entity = area.History.Get(id, version);
             *     entity = pipeline.ExecuteBeforeRevert(entity, current, contentType, context);
             *     area.Update(id, entity);
             *     entity = pipeline.ExecuteAfterRevert(entity, current, contentType, context);
             *     manager.QueueUpdate(entity);
             *     return entity;
             * }
            ***/
        }
    }

    public static class RevertPipelineExtensions
    {
        //public static Task<JObject> ExecuteRevert(this IPipelines self, string contentType, int version, JObject target, JObject current, Func<RevertPipelineContext, JObject> finalizer)
        //{
        //    var pipeline = self.Select(For.Name("Revert").And(For.ContentType(contentType)));
        //    return pipeline.Execute(new RevertPipelineContext(contentType, version, target, current), finalizer);
        //}
        //public static Task<JObject> Execute(this IJsonPipeline self, string contentType, int version, JObject target, JObject current, Func<RevertPipelineContext, JObject> finalizer)
        //{
        //    return self.Execute(new RevertPipelineContext(contentType, version, target, current), finalizer);
        //}
    }
    public class RevertPipelineContext : IJsonPipelineContext
    {
        public Guid Id { get; }
        public string ContentType { get; }
        public int Version { get; }
        public JObject Target { get; }
        public JObject Current { get; }

        public RevertPipelineContext(Guid id, string contentType, int version, JObject target, JObject current)
        {
            Id = id;
            ContentType = contentType;
            Version = version;
            Target = target;
            Current = current;
        }

        public object GetParameter(string key)
        {
            return null;
        }

        public IJsonPipelineContext Replace(params (string key, object value)[] values)
        {
            return this;
        }

        public bool TryGetValue(string key, out string value)
        {
            switch (key)
            {
                case "contentType":
                    value = ContentType;
                    return true;

                case "revert":
                    value = ContentType;
                    return true;
            }

            value = null;
            return false;
        }
    }
}