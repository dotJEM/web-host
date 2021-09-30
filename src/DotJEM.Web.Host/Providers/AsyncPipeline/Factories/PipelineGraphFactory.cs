using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public interface IPipelineGraphFactory
    {
        IList<IClassNode<T>> GetHandlers<T>();
    }

    public class PipelineGraphFactory : IPipelineGraphFactory
    {
        private readonly ConcurrentDictionary<Type, object> graphs = new();
        private readonly IPipelineHandlerCollection handlers;
        private readonly IPipelineExecutorDelegateFactory factory;

        public PipelineGraphFactory(IPipelineHandlerCollection handlers, IPipelineExecutorDelegateFactory factory)
        {
            this.handlers = handlers;
            this.factory = factory;
        }

        public IList<IClassNode<T>> GetHandlers<T>()
        {
            return (IList<IClassNode<T>>) graphs.GetOrAdd(typeof(T), _ => BuildGraph<T>(this.handlers));
        }

        private List<IClassNode<T>> BuildGraph<T>(IPipelineHandlerCollection providers)
        {
            List<IClassNode<T>> groups = new();
            foreach (IPipelineHandlerProvider provider in providers)
            {
                Type type = provider.GetType();
                PipelineFilterAttribute[] selectors = type.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();

                List<MethodNode<T>> nodes = new();
                foreach (MethodInfo method in type.GetMethods())
                {
                    PipelineFilterAttribute[] methodSelectors = method.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();
                    if (methodSelectors.Any())
                    {

                        MethodNode<T> node = factory.CreateNode<T>(provider, method, selectors.Concat(methodSelectors).ToArray());
                        nodes.Add(node);
                    }
                }
                groups.Add(new ClassNode<T>(nodes));
            }
            return groups;
        }
    }
}