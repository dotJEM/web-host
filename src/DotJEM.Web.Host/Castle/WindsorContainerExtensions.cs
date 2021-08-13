using Castle.MicroKernel.Registration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.AsyncPipeline.Handlers;

namespace DotJEM.Web.Host.Castle
{
    public static class WindsorContainerExtensions
    {
        public static IWindsorContainer RegisterPipelineStep<T>(this IWindsorContainer self) where T : IAsyncPipelineHandler
            => self.Register(Component.For<IAsyncPipelineHandler>().ImplementedBy<T>());
    }
}