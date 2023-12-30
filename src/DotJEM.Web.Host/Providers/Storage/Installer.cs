using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.Storage.Cutoff;

namespace DotJEM.Web.Host.Providers.Storage;

public class Installer : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IJsonStorageManager>().ImplementedBy<IJsonStorageManager>().LifestyleSingleton());
        container.Register(Component.For<IStorageChangeFilterHandler>().ImplementedBy<StorageChangeFilterHandler>().LifestyleSingleton());
    }
}