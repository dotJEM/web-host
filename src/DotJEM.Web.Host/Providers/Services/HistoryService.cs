using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using DotJEM.Web.Host.Providers.Concurrency;
using Newtonsoft.Json.Linq;
using static DotJEM.Web.Host.Providers.AsyncPipeline.SelectorBuilder;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IHistoryService
    {
        IStorageArea StorageArea { get; }

        JObject History(Guid id, string contentType, int version);
        JObject Revert(Guid id, string contentType, int version);

        IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        IEnumerable<JObject> Deleted(string contentType, DateTime? from = null, DateTime? to = null);
    }

    public class HistoryService : IHistoryService
    {
        private readonly IPipelines pipelines;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IAsyncPipeline pipeline;

        public IStorageArea StorageArea => area;

        public HistoryService(IStorageArea area, IStorageIndexManager manager, IAsyncPipeline pipeline)
        {
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
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

            IJsonPipeline pipeline = pipelines.Select(For.ContentType(contentType).Name("Revert"));

            pipeline.Execute(new RevertPipelineContext(contentType, version));

            JObject current = area.Get(id);
            
            //IPutContext context = pipeline.ContextFactory.CreatePutContext(contentType, current);
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
        }
    }

    public class RevertPipelineProvider
    {

    }
}