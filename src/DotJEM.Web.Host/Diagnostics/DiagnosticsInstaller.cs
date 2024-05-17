using System.Web.Http.ExceptionHandling;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Diagnostics.ExceptionLoggers;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Diagnostics.Telemetry;

namespace DotJEM.Web.Host.Diagnostics;

public class DiagnosticsInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<ILogWriterFactory>().ImplementedBy<LogWriterFactory>().LifestyleSingleton());
        container.Register(Component.For<IDiagnosticsLogger>().ImplementedBy<DiagnosticsLogger>().LifestyleTransient());
        container.Register(Component.For<IExceptionLogger>().ImplementedBy<DiagnosticsExceptionLogger>().LifestyleTransient());
        container.Register(Component.For<IExceptionHandler>().ImplementedBy<WebHostExceptionHandler>().LifestyleTransient());
        //container.Register(Component.For<ILoggerFactory>().ImplementedBy<LoggerFactory>());
        //container.Register(Component.For<ILogger>().UsingFactoryMethod(kernel => kernel.Resolve<ILoggerFactory>().Create()).LifestyleSingleton());
        //container.Register(Component.For<IPerformanceLoggingCustomDataProviderManager>()
        //    .ImplementedBy<PerformanceLoggingCustomDataProviderManager>());
        container.Register(Component.For<ITelemetry>().ImplementedBy<NullTelemetry>().LifestyleSingleton()
            .OnlyNewServices()
            .IsFallback());
    }
}