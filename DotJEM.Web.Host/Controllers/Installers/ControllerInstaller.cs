using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DotJEM.Web.Host.Controllers.Installers
{
    public class ControllerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            //container.Register(Component.For<SearchController>().LifestyleTransient());
            //container.Register(Component.For<FileController>().LifestyleTransient());
            //container.Register(Component.For<DiagnosticsController>().LifestyleTransient());
            //container.Register(Component.For<TermController>().LifestyleTransient());
        }
    }
}
