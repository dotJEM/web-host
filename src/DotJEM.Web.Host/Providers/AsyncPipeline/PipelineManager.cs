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
        ICompiledPipeline<T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : class, IPipelineContext;
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

        public ICompiledPipeline<T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : class, IPipelineContext
        {
            IUnboundPipeline<T> unbound = LookupPipeline(context, final);
            return new CompiledPipeline<T>(unbound, context);
        }

        public IUnboundPipeline<T> LookupPipeline<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : class, IPipelineContext
        {
            IPipelineGraph<T> graph = factory.GetGraph<T>();
            return (IUnboundPipeline<T>)cache.GetOrAdd(graph.Key(context), key =>
            {
                IEnumerable<MethodNode<T>> matchingNodes = graph.Nodes(context);
                return new UnboundPipeline<T>(performance, graph, matchingNodes, ctx => final(ctx as TContext));
            });
        }
    }
}