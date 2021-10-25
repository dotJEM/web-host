using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Pipelines;
using DotJEM.Pipelines.Factories;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public class AsyncPipelineInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IPipelines>().ImplementedBy<PipelineManager>().LifestyleTransient());
            container.Register(Component.For<IPipelineGraphFactory>().ImplementedBy<PipelineGraphFactory>().LifestyleTransient());
            container.Register(Component.For<IPipelineExecutorDelegateFactory>().ImplementedBy<PipelineExecutorDelegateFactory>());
            container.Register(Component.For<IPipelineHandlerCollection>().ImplementedBy<PipelineHandlerCollection>().LifestyleTransient());
        }
    }


    /* NEW CONCEPT: Named pipelines */


    //Task<JObject> Execute<TContext>(TContext context, Func<TContext, JObject> finalize) where TContext : IPipelineContext;
}
