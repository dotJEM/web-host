namespace DotJEM.Web.Host.Configuration
{
    public interface IRouteConfiguratorSetter
    {
        IRouteConfiguratorAnd Defaults(object value);
        IRouteConfiguratorAnd Constraints(object value);
    }
}