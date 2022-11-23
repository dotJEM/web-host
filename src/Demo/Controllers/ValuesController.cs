using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host;
using Newtonsoft.Json.Linq;

namespace Demo.Controllers
{
    public class StorageController : WebHostApiController
    {
        private readonly IServiceProvider<IContentService> provider;
        private readonly ConcurrentDictionary<string, IContentService> services = new ConcurrentDictionary<string, IContentService>();

        private IContentService Lookup(string area)
        {
            return services.GetOrAdd(area, a => provider.Create(area));
        }

        public StorageController(IServiceProvider<IContentService> provider)
        {
            this.provider = provider;
        }

        [HttpGet]
        public dynamic Get([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id)
        {
            JObject entity = Lookup(area).Get(id, contentType);
            if (entity == null)
            {
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public dynamic Post([FromUri] string area, [FromUri] string contentType, [FromBody] JObject entity)
        {
            if (entity == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            entity = Lookup(area).Post(contentType, entity);
            return entity;
        }

        [HttpPut]
        public dynamic Put([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id, [FromBody] JObject entity)
        {
            if (entity == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            entity = Lookup(area).Put(id, contentType, entity);
            return entity;
        }

        [HttpDelete]
        public dynamic Delete([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id)
        {
            JObject deleted = Lookup(area).Delete(id, contentType);
            if (deleted == null)
            {
                return NotFound();
            }
            return deleted;
        }

    }
}
