using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace DotJEM.Web.Host
{
    public interface IRouting
    {
        IRouting Ignore(string route);
        IRouting Default<TController>(string action = "Get");
        IRouting Api<TController>(string route, object defaults = null, object constraints = null, HttpMessageHandler handler = null);
        IRouting Page<TController>(string route, object defaults = null);
    }

    public class HttpRouting<TConfiguration> : IRouting where TConfiguration : HttpConfiguration
    {
        private readonly TConfiguration configuration;

        public HttpRouting(TConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IRouting Api<TController>(string route, object defaults = null, object constraints = null, HttpMessageHandler handler = null)
        {
            configuration.Routes.MapHttpRoute<TController>(GenerateUniqueName(), route, defaults, constraints, handler);
            return this;
        }

        public IRouting Page<TController>(string route, object defaults = null)
        {
            //Configuration.Routes.MapHttpRoute(name, routeTemplate, defaults); 
            //TODO: The RouteTable is specific to IIS, so we should use the configuration.Routes instead.
            //      but I have yet to get that to work though.
            RouteTable.Routes.MapRoute(GenerateUniqueName(), route, defaults);
            return this;
        }

        public IRouting Default<TController>(string action = "Get")
        {
            ////TODO: HACK to strongly map our Default page controller.
            string controller = typeof(TController).Name.Replace("Controller", "");
            Page<TController>("{*ignorePath}", new { action, controller });
            return this;
        }

        public IRouting Ignore(string route)
        {
            //configuration.Routes.IgnoreRoute("ignore" + route, route);
            RouteTable.Routes.IgnoreRoute(route);
            return this;
        }

        private string GenerateUniqueName()
        {
            return Guid.NewGuid().ToString();
        }
    }
}