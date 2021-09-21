using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Resolvers;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public interface IPipelineHandlerCollection : IEnumerable<IPipelineHandler>
    {
    }

    public class PipelineHandlerCollection : IPipelineHandlerCollection
    {
        private readonly List<IPipelineHandler> providers;

        public PipelineHandlerCollection(IPipelineHandler[] providers)
        {
            this.providers = OrderHandlers(providers);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IPipelineHandler> GetEnumerator()
        {
            return providers.GetEnumerator();
        }

        private List<T> OrderHandlers<T>(T[] steps)
        {
            Queue<T> queue = new(steps);
            Dictionary<Type, T> map = steps.ToDictionary(h => h.GetType());
            HashSet<Type> ordered = new();
            while (queue.Count > 0)
            {
                T handler = queue.Dequeue();
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
            return ordered.Select(type => map[type]).ToList();
        }
    }

    public interface IPipelineGraphFactory
    {
        List<IClassNode<T>> BuildHandlerGraph<T>(IPipelineHandlerCollection providers);
    }

    public class PipelineGraphFactory : IPipelineGraphFactory
    {
        public List<IClassNode<T>> BuildHandlerGraph<T>(IPipelineHandlerCollection providers)
        {
            List<IClassNode<T>> groups = new();
            foreach (IPipelineHandler provider in providers)
            {
                Type type = provider.GetType();
                PipelineFilterAttribute[] selectors = type.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();

                List<MethodNode<T>> nodes = new();
                foreach (MethodInfo method in type.GetMethods())
                {
                    PipelineFilterAttribute[] methodSelectors = method.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();
                    if (methodSelectors.Any())
                    {
                        MethodNode<T> node = new PipelineExecutorDelegateFactory().CreateNode<T>(provider, method, selectors.Concat(methodSelectors).ToArray());
                        nodes.Add(node);
                    }
                }
                groups.Add(new ClassNode<T>(nodes));
            }
            return groups;
        }
    }
}