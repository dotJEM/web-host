using System;
using System.Threading.Tasks;
using System.Web.Http;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Services;
using Newtonsoft.Json.Linq;

namespace Demo.Controllers
{
    public class ContentController : WebHostApiController
    {
        private readonly IServiceProvider<IContentService> provider;

        public ContentController(IServiceProvider<IContentService> provider)
        {
            this.provider = provider;
        }

        [HttpGet]
        public async Task<object> Get([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id)
        {
            IContentService service = provider.Create(area);
            JObject entity = await service.GetAsync(id, contentType);
            if (entity == null)
            {
                return NotFound($"Could not find content of type '{contentType}' with id [{id}] in area 'content'.");
            }
            return entity;
        }

        [HttpPost]
        public async Task<object> Post([FromUri] string area, [FromUri] string contentType, [FromBody] JObject doc)
        {
            if (doc == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            return await provider.Create(area).PostAsync(contentType, doc);
        }

        [HttpPut]
        public async Task<object> Put([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id, [FromBody] JObject doc)
        {
            if (doc == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            return await provider.Create(area).PutAsync(id, contentType, doc);
        }

        [HttpPatch]
        public async Task<object> Patch([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id, [FromBody] JObject doc)
        {
            IContentService storage = provider.Create(area);
            JObject previous = await storage.GetAsync(id, contentType);
            previous.Merge(doc);
            if (doc == null)
            {
                return BadRequest("Request did not contain any content.");
            }
            return await storage.PutAsync(id, contentType, previous);
        }

        [HttpDelete]
        public async Task<object> Delete([FromUri] string area, [FromUri] string contentType, [FromUri] Guid id)
        {
            JObject deleted = await provider.Create(area).DeleteAsync(id, contentType);
            if (deleted == null)
            {
                return NotFound($"Could not delete content with id [{id}] in area 'content' as it could not be found.");
            }
            return deleted;
        }
    }
}