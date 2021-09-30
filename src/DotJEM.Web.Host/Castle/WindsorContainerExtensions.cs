using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.AsyncPipeline;

namespace DotJEM.Web.Host.Castle
{
    public static class WindsorContainerExtensions
    {
        public static IWindsorContainer RegisterPipelineHandlerProvider<T>(this IWindsorContainer self, Func<ComponentRegistration<IPipelineHandlerProvider>, ComponentRegistration<IPipelineHandlerProvider>> postConfig = null) where T : IPipelineHandlerProvider
        {
            ComponentRegistration<IPipelineHandlerProvider> registration = Component.For<IPipelineHandlerProvider>().ImplementedBy<T>();
            registration = postConfig?.Invoke(registration) ?? registration;
            return self.Register(registration);
        }
    }
}