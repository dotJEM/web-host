using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IContentService
    {
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

        public ContentService(IStorageIndex index, IStorageArea area, IStorageIndexManager manager, IPipeline pipeline)
        {
            this.index = index;
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
        }

        public IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20)
        {
            var res = index.Search("contentType: " + contentType)
                .Skip(skip).Take(take)
                .Select(hit => hit.Json)
                .Select(json => pipeline.ExecuteAfterGet(json, contentType)) //Executes the pipeline for each element found
                .Cast<JObject>().ToArray();

            return res;

            //TODO: Execute pipeline for array
            //TODO: Paging and other neat stuff...
            //TODO: Use search for optimized performance!...
            //return area.Get(contentType).Skip(skip).Take(take);
        }

        public JObject Get(Guid id, string contentType)
        {
            //TODO: Use search for optimized performance!...
            JObject entity = area.Get(id);

            //TODO: Throw exception if not found?
            //if (entity == null)
            //    return Request.CreateResponse(HttpStatusCode.NotFound, "Could not find cotent of type '" + contentType + "' with id [" + id + "] in area '" + Area.Name + "'");

            return pipeline.ExecuteAfterGet(entity, contentType);
        }

        public JObject Post(string contentType, JObject entity)
        {
            entity = pipeline.ExecuteBeforePost(entity, contentType);

            entity = area.Insert(contentType, entity);

            entity = pipeline.ExecuteAfterPost(entity, contentType);

            manager.QueueUpdate(entity);

            //index.Write(entity);
            return entity;
        }

        public JObject Put(Guid id, string contentType, JObject entity)
        {
            entity = pipeline.ExecuteBeforePut(entity, contentType);

            entity = area.Update(id, entity);

            entity = pipeline.ExecuteAfterPut(entity, contentType);

            manager.QueueUpdate(entity);
            //index.Write(entity);
            return entity;
        }

        public JObject Delete(Guid id, string contentType)
        {
            JObject deleted = area.Delete(id);
            if (deleted == null)
                return null;

            //TODO: Throw exception if not found?
            //    if (deleted == null)
            //        return Request.CreateResponse(HttpStatusCode.NotFound, "Could not delete cotent with id [" + id + "] in area '" + Area.Name + "' as it could not be found.");
            manager.QueueDelete(deleted);

            //index.Delete(deleted);
            return pipeline.ExecuteAfterDelete(deleted, contentType);
        }
    }
}