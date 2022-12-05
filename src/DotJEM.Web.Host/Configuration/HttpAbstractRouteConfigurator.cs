using System;

namespace DotJEM.Web.Host.Configuration;

public abstract class HttpAbstractRouteConfigurator : IRouteConfigurator
{
    protected string Name { get; private set; }
    protected string Route { get; private set; }

    protected HttpRouterConfigurator Router { get; private set; }

    protected HttpAbstractRouteConfigurator(string route, HttpRouterConfigurator router)
    {
        Router = router;

        Route = route;
        Name = Guid.NewGuid().ToString();
    }

    public IRouteConfigurator Named(string name)
    {
        Name = name;
        return this;
    }

    public virtual IRouter To<TController>()
    {
        return To<TController>(x => {});
    }

    public virtual IRouter To<TController>(Action<IRouteConfiguratorExtras> config)
    {
        RouteConfiguratorExtras extras = new RouteConfiguratorExtras();
        config(extras);

        return Router.AddRoute(Name, new WebRoute<TController>(Route, extras.BuildDefaults(), extras.BuildConstraints(), null, null));
    }

    public virtual IRouter Through()
    {
        return Router.IgnoreRoute(Name, Route);
    }
}