using System;
using System.Collections.Concurrent;
using System.Web.Http;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Services;
using Newtonsoft.Json.Linq;

namespace Demo.Server
{
    public class DemoContentController : WebHostHubController<DemoContentHub>
    {
        private readonly IServiceProvider<IContentService> provider;
        private readonly ConcurrentDictionary<string, IContentService> services = new ConcurrentDictionary<string, IContentService>();

        private IContentService Lookup(string area)
        {
            return services.GetOrAdd(area, a => provider.Create(area));
        }

        public DemoContentController(IServiceProvider<IContentService> provider)
        {
            this.provider = provider;
        }

        [HttpGet]
        public dynamic Get([FromUri]string area, [FromUri]string contentType, [FromUri]int skip = 0, [FromUri]int take = 20)
        {
            return Lookup(area).Get(contentType, skip, take);
        }

        [HttpGet]
        public dynamic Get([FromUri]string area, [FromUri]string contentType, [FromUri]Guid id)
        {
            JObject entity = Lookup(area).Get(id, contentType);
            if (entity == null)
            {
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public dynamic Post([FromUri]string area, [FromUri]string contentType, [FromBody]JObject entity)
        {
            if (entity == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            entity = Lookup(area).Post(contentType, entity);
            Hub.Clients.All.post(entity);
            return entity;
        }

        [HttpPut]
        public dynamic Put([FromUri]string area, [FromUri]string contentType, [FromUri]Guid id, [FromBody]JObject entity)
        {
            if (entity == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            entity = Lookup(area).Put(id, contentType, entity);
            Hub.Clients.All.put(entity);
            return entity;
        }

        [HttpDelete]
        public dynamic Delete([FromUri]string area, [FromUri]string contentType, [FromUri]Guid id)
        {
            JObject deleted = Lookup(area).Delete(id, contentType);
            if (deleted == null)
            {
                return NotFound();
            }
            Hub.Clients.All.delete(deleted);
            return deleted;
        }


    }
}