using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DotJEM.Web.Host.DataCleanup;

public class CleanupInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IDataCleanupManager>().ImplementedBy<DataCleanupManager>().LifestyleSingleton());
    }
}