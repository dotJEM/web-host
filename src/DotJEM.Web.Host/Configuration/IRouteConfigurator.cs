using System;

namespace DotJEM.Web.Host.Configuration;

public interface IRouteConfigurator
{
    IRouteConfigurator Named(string name);

    IRouter To<TController>();
    IRouter To<TController>(Action<IRouteConfiguratorExtras> config);
    IRouter Through();
}