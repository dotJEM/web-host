using System;
using System.Net.Http;
using System.Web.Http.Routing;

namespace DotJEM.Web.Host
{
    public interface IWebRoute
    {
        string ControllerName { get; }
        Type ControllerType { get; }
    }

    public class WebRoute<TController> : HttpRoute, IWebRoute
    {
        public string ControllerName { get; private set; }
        public Type ControllerType { get; private set; }

        public WebRoute(string routeTemplate, HttpRouteValueDictionary defaults, HttpRouteValueDictionary constraints, HttpRouteValueDictionary dataTokens, HttpMessageHandler handler)
            : base(routeTemplate, defaults, constraints, dataTokens, handler)
        {
            ControllerType = typeof(TController);
            ControllerName = ControllerType.FullName;
        }
    }
}