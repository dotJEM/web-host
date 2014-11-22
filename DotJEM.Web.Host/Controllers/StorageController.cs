using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Controllers
{
    public abstract class StorageController : ApiController
    {
        protected IStorageIndex Index { get; private set; }
        protected IStorageArea Area { get; private set; }
        protected IStorageContext Storage { get; private set; }

        protected StorageController(IStorageContext storage, IStorageIndex index, string areaName)
        {
            Index = index;
            Storage = storage;

            Area = storage.Area(areaName);
        }

        [HttpGet]
        public virtual dynamic Get([FromUri]string contentType, [FromUri]int skip = 0, [FromUri]int take = 20)
        {
            //TODO: Paging and other neat stuff...
            return Area.Get(contentType).Skip(skip).Take(take);
        }

        [HttpGet]
        public virtual dynamic Get([FromUri]Guid id, [FromUri]string contentType)
        {
            JObject entity = Area.Get(id);
            if (entity == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, "Could not find cotent of type '" + contentType + "' with id [" + id + "] in area '" + Area.Name + "'");

            return entity;
        }

        [HttpPost]
        public virtual dynamic Post([FromUri]string contentType, [FromBody]JObject entity)
        {
            entity = Area.Insert(contentType, entity);
            Index.Write(entity);
            return entity;
        }

        [HttpPut]
        public virtual dynamic Put([FromUri]Guid id, [FromUri]string contentType, [FromBody]JObject entity)
        {
            entity = Area.Update(id, entity);
            Index.Write(entity);
            return entity;
        }

        [HttpPatch]
        public virtual dynamic Patch([FromUri]Guid id, [FromUri]string contentType, [FromBody]JObject entity)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        public virtual dynamic Delete([FromUri]Guid id)
        {
            JObject deleted = Area.Delete(id);
            if (deleted == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, "Could not delete cotent with id [" + id + "] in area '" + Area.Name + "' as it could not be found.");

            Index.Delete(deleted);
            return deleted;
        }
    }
}