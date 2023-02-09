using Microsoft.Owin;
using Owin;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using Castle.Windsor;
using DotJEM.Web.Host;

[assembly: OwinStartup(typeof(Demo.Startup))]

namespace Demo
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            DemoHost host = new DemoHost(config);
            host.Start();

            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
        }
    }

    //public static class WindsorMiddlewareExtensions
    //{
    //    /// <summary/>
    //    public static IAppBuilder UseWindsor(this IAppBuilder app, HttpConfiguration config, IWindsorContainer container)
    //    {
    //        WindsorDependencyResolver windsorResolver = new WindsorDependencyResolver(container);
    //        config.Services.Replace(typeof(IHttpControllerSelector), new WindsorControllerSelector(config, container));
    //        config.Services.Replace(typeof(IHttpControllerActivator), new EWareControllerActivator(container));
    //        config.DependencyResolver = windsorResolver;
    //        return app.Use<WindsorMiddleware>(windsorResolver);
    //    }

    //    /// <summary/>
    //    private const string DependencyResolverKey = "WindsorDependencyResolver.5e862c66ac";

    //    /// <summary/>
    //    public static void SetDependencyResolver(this IOwinContext context, IDependencyResolver resolver)
    //    {
    //        context.Environment[DependencyResolverKey] = resolver;
    //    }

    //    /// <summary/>
    //    public static IDependencyResolver GetDependencyResolver(this IOwinContext context)
    //    {
    //        return context.Environment[DependencyResolverKey] as IDependencyResolver;
    //    }

    //    /// <summary/>
    //    public static TService GetService<TService>(this IDependencyResolver resolver) where TService : class
    //    {
    //        return resolver.GetService(typeof(TService)) as TService;
    //    }
    //}
}
