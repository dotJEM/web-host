using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost(GlobalConfiguration.Configuration).Start();
        }
    }
    public class DemoHost : WebHost
    {
        public DemoHost(HttpConfiguration configuration) : base(configuration)
        {
        }

        protected override void Configure(IRouter router)
        {
            router.Route("api/exception").To<ExceptionController>();
            router.Route("api/storage/{area}/{contentType}/{id}")
                .To<StorageController>(x => x.Set.Defaults(new { id = RouteParameter.Optional }));
            router.Route("api/search").To<SearchController>();
            router.Default("Index").To<IndexController>();
        }

        protected override void Configure(IWindsorContainer container)
        {
            container.Register(Component.For<ExceptionController>().LifestyleTransient());
            container.Register(Component.For<IndexController>().LifestyleTransient());
            container.Register(Component.For<SearchController>().LifestyleTransient());
            container.Register(Component.For<StorageController>().LifestyleTransient());
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<MyCustomExceptionHandler>());
        }
    }
}
