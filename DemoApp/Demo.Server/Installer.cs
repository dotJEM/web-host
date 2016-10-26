using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Demo.Server
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<DemoContentController>().LifestyleTransient());
            container.Register(Component.For<DemoSearchController>().LifestyleTransient());
        }
    }
}