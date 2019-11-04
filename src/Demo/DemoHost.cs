using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;

namespace Demo
{
    public class DemoHost : WebHost
    {
        public DemoHost(HttpConfiguration configuration) : base(configuration)
        {
        }

        protected override void Configure(IRouter router)
        {
            router.Route("exception").To<ExceptionController>();
            router.Route("identity").To<IdentityController>();
            
            router
                .Default("Index")
                .To<IndexController>();
        }

        protected override void Configure(IWindsorContainer container)
        {
            container.Register(Component.For<ExceptionController>().LifestyleTransient());
            container.Register(Component.For<IndexController>().LifestyleTransient());
            container.Register(Component.For<IdentityController>().LifestyleTransient());
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<MyCustomExceptionHandler>());
        }
    }
}