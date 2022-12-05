using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace DotJEM.Web.Host.Configuration;

public class HttpDefaultRouteConfigurator : HttpAbstractRouteConfigurator
{
    private readonly string action;

    public HttpDefaultRouteConfigurator(string action, HttpRouterConfigurator router)
        : base("{*ignorePath}", router)
    {
        this.action = action;
    }

    public override IRouter To<TController>(Action<IRouteConfiguratorExtras> config)
    {
        RouteConfiguratorExtras extras = new RouteConfiguratorExtras();
        config(extras);

        var defaults = extras.BuildDefaults();
        defaults["action"] = action;
        defaults["controller"] = typeof(TController).Name.Replace("Controller", "");

        //TODO: Bad use of RouteTable here, we should use the configuration.Routes instead.
        //      but I have yet to get that to work though.
        RouteTable.Routes.MapRoute(Name, Route, defaults);
        return Router;
    }
}