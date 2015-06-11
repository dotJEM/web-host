using System.Web.Http.ExceptionHandling;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Diagnostics.ExceptionLoggers;

namespace DotJEM.Web.Host.Diagnostics
{
    public class DiagnosticsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<ILogWriterFactory>().ImplementedBy<LogWriterFactory>().LifestyleTransient());
            container.Register(Component.For<IPerformanceLogger>().ImplementedBy<PerformanceLogger>().LifestyleTransient());
            container.Register(Component.For<IDiagnosticsLogger>().ImplementedBy<DiagnosticsLogger>().LifestyleTransient());
            container.Register(Component.For<IExceptionLogger>().ImplementedBy<DiagnosticsExceptionLogger>().LifestyleTransient());
            container.Register(Component.For<IExceptionHandler>().ImplementedBy<WebHostExceptionHandler>().LifestyleTransient());
        }
    }
}