namespace DotJEM.Web.Host.Configuration
{
    public interface IRouter
    {
        IRouteConfigurator Route(string route);
        IRouteConfigurator Default(string action = "Get");
        IRouteConfigurator Otherwise(string action = "Get");
    }
}