using Castle.MicroKernel.Registration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.AsyncPipeline;

namespace DotJEM.Web.Host.Castle
{
    public static class WindsorContainerExtensions
    {
        public static IWindsorContainer RegisterPipelineHandler<T>(this IWindsorContainer self) where T : IAsyncPipelineHandler
            => self.Register(Component.For<IAsyncPipelineHandler>().ImplementedBy<T>());
    }
}