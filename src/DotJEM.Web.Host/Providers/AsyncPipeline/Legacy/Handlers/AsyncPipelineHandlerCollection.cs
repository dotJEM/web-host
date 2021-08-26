using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Resolvers;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public class AsyncPipelineHandlerCollection : IAsyncPipelineHandlerCollection
    {
        private readonly ILogger performance;
        private readonly IAsyncPipelineHandler termination;
        private readonly IAsyncPipelineHandler[] handlers;
        private readonly ConcurrentDictionary<string, IPipeline> cache = new();

        public AsyncPipelineHandlerCollection(ILogger performance, IAsyncPipelineHandler[] steps, IAsyncPipelineHandler termination)
        {
            this.performance = performance;
            this.termination = termination;
            handlers = OrderHandlers(steps);
        }

        public IPipeline For(string contentType)
        {
            return cache.GetOrAdd(contentType, CreateBoundPipeline);
        }

        private IPipeline CreateBoundPipeline(string contentType)
        {
            IPipelineBuidler pipeline = performance.IsEnabled()
                ? new PerformanceTrackingPipelineBuilder(performance, termination)
                : new PipelineBuilder(termination);
            Stack<IAsyncPipelineHandler> filtered = new(handlers.Where(x => CheckHandler(x, contentType)));
            while (filtered.Count > 0) pipeline = pipeline.PushAfter(filtered.Pop());
            return pipeline.Build();
        }

        private bool CheckHandler(IAsyncPipelineHandler arg, string contentType)
        {
            //TODO: Enforce at least one Attribute.
            return arg
                .GetType()
                .GetCustomAttributes(typeof(ContentTypeFilterAttribute), true)
                .Cast<ContentTypeFilterAttribute>()
                .Any(att => att.IsMatch(contentType));
        }

        private static IAsyncPipelineHandler[] OrderHandlers(IAsyncPipelineHandler[] steps)
        {
            Queue<IAsyncPipelineHandler> queue = new Queue<IAsyncPipelineHandler>(steps);
            Dictionary<Type, IAsyncPipelineHandler> map = steps.ToDictionary(h => h.GetType());
            var ordered = new HashSet<Type>();
            while (queue.Count > 0)
            {
                IAsyncPipelineHandler handler = queue.Dequeue();
                Type handlerType = handler.GetType();
                PipelineDepencency[] dependencies = PipelineDepencency.GetDepencencies(handler);
                if (dependencies.Length < 1 || dependencies.All(d => ordered.Contains(d.Type)))
                {
                    ordered.Add(handlerType);
                }
                else
                {
                    IEnumerable<PipelineDepencency> unknownDependencies = dependencies
                        .Where(dep => !map.ContainsKey(dep.Type))
                        .ToArray();
                    if (unknownDependencies.Any())
                    {
                        string message = $"{handlerType.FullName} has dependencies to be satisfied, missing dependencies:" +
                                         $"\n\r{string.Join("\n\r - ", unknownDependencies.Select(d => d.Type.FullName))}";
                        throw new DependencyResolverException(message);
                    }
                    queue.Enqueue(handler);
                }
            }
            return ordered.Select(type => map[type]).ToArray();
        }
    }
}