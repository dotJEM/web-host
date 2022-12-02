using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Resolvers;

namespace DotJEM.Web.Host.Providers.Pipeline;

public class PipelineHandlerSet : IPipelineHandlerSet
{
    private readonly IEnumerable<IPipelineHandler> handlers;

    public PipelineHandlerSet(IPipelineHandler[] steps)
    {
        handlers = OrderHandlers(steps);
    }

    private static IEnumerable<IPipelineHandler> OrderHandlers(IPipelineHandler[] steps)
    {
        Queue<IPipelineHandler> queue = new Queue<IPipelineHandler>(steps);
        var map = steps.ToDictionary(h => h.GetType());
        var ordered = new HashSet<Type>();
        while (queue.Count > 0)
        {
            IPipelineHandler handler = queue.Dequeue();
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
                    string message = handlerType.FullName + " has dependencies to be satisfied, missing dependencies:\n\r"
                                                          + string.Join("\n\r - ", unknownDependencies.Select(d => d.Type.FullName));
                    throw new DependencyResolverException(message);
                }
                queue.Enqueue(handler);
            }
        }
        return ordered.Select(type => map[type]).ToList();
    }


    public IEnumerator<IPipelineHandler> GetEnumerator()
    {
        return handlers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}