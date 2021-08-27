using Castle.MicroKernel.Registration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.AsyncPipeline;

namespace DotJEM.Web.Host.Castle
{
    public static class WindsorContainerExtensions
    {
        public static IWindsorContainer RegisterPipelineStep<T>(this IWindsorContainer self) where T : IJsonPipelineHandler
            => self.Register(Component.For<IJsonPipelineHandler>().ImplementedBy<T>());
    }
}