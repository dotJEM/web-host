using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IContentService
    {
        IStorageArea StorageArea { get; }
        //TODO: Use a Content Result
        IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20);
        JObject Get(Guid id, string contentType);

        JObject Post(string contentType, JObject entity);
        JObject Put(Guid id, string contentType, JObject entity);

        JObject Delete(Guid id, string contentType);
    }

    //TODO: Apply Pipeline for all requests.
    public class ContentService : IContentService
    {
        private readonly IStorageIndex index;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IPipeline pipeline;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageIndex index, IStorageArea area, IStorageIndexManager manager, IPipeline pipeline)
        {
            this.index = index;
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
        }

        public IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20)
        {
            JObject[] res = index.Search("contentType: " + contentType)
                .Skip(skip).Take(take)
                .Select(hit => hit.Json)
                //Note: Execute the pipeline for each element found
                .Select(json => pipeline.ExecuteAfterGet(json, contentType, new PipelineContext()))
                .Cast<JObject>().ToArray();

            return res;

            //TODO: Execute pipeline for array
            //TODO: Paging and other neat stuff...
            //TODO: Use search for optimized performance!...
        }

        public JObject Get(Guid id, string contentType)
        {
            PipelineContext context = new PipelineContext();
            //TODO: Use search for optimized performance!...
            //TODO: Throw exception if not found?
            JObject entity = area.Get(id);
            return pipeline.ExecuteAfterGet(entity, contentType, context);
        }

        public JObject Post(string contentType, JObject entity)
        {
            PipelineContext context = new PipelineContext();
            entity = pipeline.ExecuteBeforePost(entity, contentType, context);
            entity = area.Insert(contentType, entity);
            entity = pipeline.ExecuteAfterPost(entity, contentType, context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public JObject Put(Guid id, string contentType, JObject entity)
        {
            var prev = area.Get(id);
            PipelineContext context = new PipelineContext();
            entity = pipeline.ExecuteBeforePut(entity, prev, contentType, context);
            entity = area.Update(id, entity);
            entity = pipeline.ExecuteAfterPut(entity, prev, contentType, context);
            manager.QueueUpdate(entity);
            return entity;
        }

        public JObject Delete(Guid id, string contentType)
        {
            PipelineContext context = new PipelineContext();
            pipeline.ExecuteBeforeDelete(area.Get(id), contentType, context);
            JObject deleted = area.Delete(id);
            //TODO: Throw exception if not found?
            if (deleted == null)
                return null;

            manager.QueueDelete(deleted);
            return pipeline.ExecuteAfterDelete(deleted, contentType, context);
        }
    }
}