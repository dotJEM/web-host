using System;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;
using Newtonsoft.Json.Linq;

namespace Demo
{
    public class DemoHost : WebHost
    {
        public DemoHost(HttpConfiguration configuration) : base(configuration)
        {
        }

        protected override void Configure(IRouter router)
        {
            router
                .Route("api/v1/exception").To<ExceptionController>()
                .Route("api/v1/content/{area}/{contentType}/{id}").To<ContentController>(extras => extras.Set.Defaults(new { id = RouteParameter.Optional}))
                ;
            router.Default("Index").To<IndexController>();
        }

        protected override void Configure(IWindsorContainer container)
        {
            container.Register(Component.For<ExceptionController>().LifestyleTransient());
            container.Register(Component.For<IndexController>().LifestyleTransient());
            container.Register(Component.For<ContentController>().LifestyleTransient());
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<MyCustomExceptionHandler>());
            
            container.RegisterPipelineStep<ExampleHandler>();

        }

        //protected override void Configure(IPipelines pipeline)
        //{
        //    base.Configure(pipeline);
        //}
    }

    [ContentTypeFilter(".*")]
    public class ExampleHandler : IPipelineHandler
    {
        [HttpMethodFilter("GET")]
        public async Task<JObject> Get(Guid id, IPipelineContext context, INext<Guid> next)
        {
            JObject entity = await next.Invoke().ConfigureAwait(false);
            entity["foo"] = "HAHA";
            return entity;
        }

        [HttpMethodFilter("GET")]
        public async Task<JObject> Get2(Guid id, IPipelineContext context, INext<Guid> next)
        {
            JObject entity = await next.Invoke().ConfigureAwait(false);
            entity["foo2"] = "HAHA2";
            return entity;
        }
    }
}