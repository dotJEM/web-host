using System.Web.Http;

namespace DotJEM.Web.Host.Configuration
{
    public class HttpRouterConfigurator : IRouter
    {
        private readonly HttpRouteCollection routes;

        public HttpRouterConfigurator(HttpRouteCollection routes)
        {
            this.routes = routes;
        }

        public IRouteConfigurator Route(string route)
        {
            return new HttpApiRouteConfigurator(route, this);
        }

        public IRouteConfigurator Default(string action = "Get")
        {
            return new HttpDefaultRouteConfigurator(action, this);
        }

        public IRouteConfigurator Otherwise(string action = "Get")
        {
            return Default(action);
        }

        internal HttpRouterConfigurator AddRoute<TController>(string name, WebRoute<TController> route)
        {
            routes.Add(name, route);
            return this;
        }

        internal HttpRouterConfigurator IgnoreRoute(string name, string route)
        {
            routes.IgnoreRoute(name, route);
            return this;
        }
    }
}