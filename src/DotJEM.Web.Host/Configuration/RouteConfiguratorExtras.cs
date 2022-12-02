using System.Web.Http.Routing;

namespace DotJEM.Web.Host.Configuration;

public class RouteConfiguratorExtras : IRouteConfiguratorExtras, IRouteConfiguratorSetter, IRouteConfiguratorAnd
{
    private object defaults;
    private object constraints;

    public IRouteConfiguratorSetter Set { get { return this; } }
    public IRouteConfiguratorSetter And { get { return this; } }

    public IRouteConfiguratorAnd Defaults(object value)
    {
        defaults = value;
        return this;
    }

    public IRouteConfiguratorAnd Constraints(object value)
    {
        constraints = value;
        return this;
    }

    public HttpRouteValueDictionary BuildDefaults()
    {
        return new HttpRouteValueDictionary(defaults);
    }

    public HttpRouteValueDictionary BuildConstraints()
    {
        return new HttpRouteValueDictionary(constraints);
    }
}