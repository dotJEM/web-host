using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
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

        public ContentService(IStorageIndex index, IStorageArea area)
        {
            this.index = index;
            this.area = area;
        }

        public IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20)
        {
            //TODO: Paging and other neat stuff...
            //TODO: Use search for optimized performance!...
            return area.Get(contentType).Skip(skip).Take(take);
        }

        public JObject Get(Guid id, string contentType)
        {
            //TODO: Use search for optimized performance!...
            JObject entity = area.Get(id);

            //TODO: Throw exception if not found?
            //if (entity == null)
            //    return Request.CreateResponse(HttpStatusCode.NotFound, "Could not find cotent of type '" + contentType + "' with id [" + id + "] in area '" + Area.Name + "'");

            return entity;
        }

        public JObject Post(string contentType, JObject entity)
        {
            entity = area.Insert(contentType, entity);
            index.Write(entity);
            return entity;
        }

        public JObject Put(Guid id, string contentType, JObject entity)
        {
            entity = area.Update(id, entity);
            index.Write(entity);
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

            index.Delete(deleted);
            return deleted;
        }
    }
}