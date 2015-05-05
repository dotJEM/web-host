using System.Web.Http.ExceptionHandling;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DotJEM.Web.Host.Diagnostics
{
    public class DiagnosticsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IDiagnosticsLogger>().ImplementedBy<DiagnosticsLogger>().LifestyleTransient());
            container.Register(Component.For<IExceptionLogger>().ImplementedBy<DiagnosticsExceptionLogger>().LifestyleTransient());
        }
    }
}