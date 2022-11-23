using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IStorageIndexManager>().ImplementedBy<StorageIndexManager>().LifestyleSingleton());
            container.Register(Component.For<IStorageManager>().ImplementedBy<StorageManager>().LifestyleSingleton());
            container.Register(Component.For<IStorageCutoff>().ImplementedBy<StorageCutoff>().LifestyleSingleton());

        }
    }
}