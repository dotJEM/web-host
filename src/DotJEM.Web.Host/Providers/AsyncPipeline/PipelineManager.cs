using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Common;
using DotJEM.Web.Host.Providers.AsyncPipeline.Factories;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public interface IPipelines
    {
        ICompiledPipeline<T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : IPipelineContext;
    }

    public class PipelineManager : IPipelines
    {
        private readonly ILogger performance;
        private readonly IPipelineGraphFactory factory;
        private readonly ConcurrentDictionary<string, object> cache = new();

        public PipelineManager(ILogger performance, IPipelineGraphFactory factory)
        {
            this.performance = performance;
            this.factory = factory;
        }

        public ICompiledPipeline<T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : IPipelineContext
        {
            IUnboundPipeline<TContext, T> unbound = LookupPipeline(context, final);
            return new CompiledPipeline<TContext, T>(unbound, context);
        }

        public IUnboundPipeline<TContext, T> LookupPipeline<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : IPipelineContext
        {
            IPipelineGraph<T> graph = factory.GetGraph<T>();
            return (IUnboundPipeline<TContext, T>)cache.GetOrAdd(graph.Key(context), key =>
            {
                IEnumerable<MethodNode<T>> matchingNodes = graph.Nodes(context);
                return new UnboundPipeline<TContext, T>(performance, graph, matchingNodes, final);
            });
        }
    }
}