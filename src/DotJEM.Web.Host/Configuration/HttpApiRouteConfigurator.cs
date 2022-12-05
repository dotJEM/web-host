namespace DotJEM.Web.Host.Configuration;

public class HttpApiRouteConfigurator : HttpAbstractRouteConfigurator
{
    public HttpApiRouteConfigurator(string route, HttpRouterConfigurator router)
        : base(route, router)
    {
    }
}