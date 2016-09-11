using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Searching;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Services;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Linq;

namespace Demo.Server
{

    public class DemoHost<TDefaultController> : WebHost
    {
        protected override void Configure(IWindsorContainer container)
        {
            container.Install(FromAssembly.InThisApplication());
        }

        protected override void Configure(IRouter router)
        {
            router
                .Route("assets/{*ignorePath}").Through()
                .Route("api/search").To<DemoSearchController>()
                .Route("api/{area}/{contentType}/{id}").To<DemoContentController>(x => x.Set.Defaults(new { id = RouteParameter.Optional }))
                .Default().To<TDefaultController>();
        }
    }
    public class LeoSearchResult
    {
        public long Take { get; set; }
        public long Skip { get; set; }
        public long Count { get; set; }

        public IEnumerable<object> Results { get; set; }

        public LeoSearchResult(ISearchResult result, long take, long skip)
        {
            Take = take;
            Skip = skip;
            Results = result.Select<IHit, object>(hit => hit.Json).ToArray();
            Count = result.TotalCount;
        }
    }

    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<DemoContentController>().LifestyleTransient());
            container.Register(Component.For<DemoSearchController>().LifestyleTransient());
        }
    }

    public class DemoSearchController : WebHostApiController
    {
        private readonly IStorageIndex index;

        public DemoSearchController(IStorageIndex index)
        {
            this.index = index;
        }

        [HttpGet]
        public dynamic Get([FromUri]string query, [FromUri]int skip = 0, [FromUri]int take = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Must specify a query.");
            }

            try
            {
                ISearchResult result = index
                    .Search(query)
                    .Skip(skip)
                    .Take(take);
                return new LeoSearchResult(result, take, skip);
            }
            catch (ParseException ex)
            {
                return BadRequest("Query is invalid: " + ex.Message);
            }

        }

    }

    [HubName("content")]
    public class DemoContentHub : Hub { }
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
