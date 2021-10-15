using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public interface IPipelineGraph
    {
        string Key(IPipelineContext context);
        JObject Performance(IPipelineContext context);
    }

    public interface IPipelineGraph<T> : IPipelineGraph
    {
        IEnumerable<MethodNode<T>> Nodes(IPipelineContext context);
    }

    public class PipelineGraph<T> : IPipelineGraph<T>
    {
        private readonly List<IClassNode<T>> nodes;
        private readonly Func<IPipelineContext, string> keyGenerator;
        private readonly Func<IPipelineContext, JObject> perfGenerator;

        public PipelineGraph(List<IClassNode<T>> nodes)
        {
            this.nodes = nodes;

            SpyingContext spy = new();
            keyGenerator = spy.CreateKeyGenerator();
            perfGenerator = spy.CreatePerfGenerator();
        }

        public string Key(IPipelineContext context) => keyGenerator(context);
        public JObject Performance(IPipelineContext context) => perfGenerator(context);

        public IEnumerable<MethodNode<T>> Nodes(IPipelineContext context)
        {
            return nodes.SelectMany(n => n.For(context));
        }

        private class SpyingContext : PipelineContext
        {
            private readonly HashSet<string> parameters = new();
            private readonly SHA256CryptoServiceProvider provider = new();
            private readonly Encoding encoding = Encoding.UTF8;

            public override bool TryGetValue(string key, out object value)
            {
                parameters.Add(key);
                value = "";
                return true;
            }

            public Func<IPipelineContext, string> CreateKeyGenerator()
            {
                return context =>
                {
                    if (context == null) throw new ArgumentNullException(nameof(context));
                    //TODO: This looks expensive. Perhaps we could cut some corners?
                    IEnumerable<byte> bytes = parameters
                        .SelectMany(key => context.TryGetValue(key, out object value) ? encoding.GetBytes(value.ToString()) : Array.Empty<byte>());
                    byte[] hash = provider.ComputeHash(bytes.Concat(encoding.GetBytes(context.GetType().FullName)).ToArray());
                    return string.Join("", hash.Select(b => b.ToString("X2")));
                };
            }

            public Func<IPipelineContext, JObject> CreatePerfGenerator()
            {
                return context =>
                {
                    return parameters.Aggregate(new JObject() { ["$$context"] = context.GetType().FullName }, (obj, key) =>
                    {
                        if (context.TryGetValue(key, out object value))
                            obj[key] = value.ToString();
                        return obj;
                    });
                };
            }
        }

    }

    public interface IPipelineGraphFactory
    {
        IPipelineGraph<T> GetGraph<T>();
    }

    public class PipelineGraphFactory : IPipelineGraphFactory
    {
        private readonly IPipelineHandlerCollection handlers;
        private readonly IPipelineExecutorDelegateFactory factory;

        private readonly ConcurrentDictionary<Type, IPipelineGraph> graphs = new();

        public PipelineGraphFactory(IPipelineHandlerCollection handlers, IPipelineExecutorDelegateFactory factory)
        {
            this.handlers = handlers;
            this.factory = factory;
        }

        public IPipelineGraph<T> GetGraph<T>()
        {
            return (IPipelineGraph<T>)graphs.GetOrAdd(typeof(T), _ => BuildGraph<T>(this.handlers));
        }

        private IPipelineGraph BuildGraph<T>(IPipelineHandlerCollection providers)
        {
            List<IClassNode<T>> groups = new();
            foreach (IPipelineHandlerProvider provider in providers)
            {
                Type type = provider.GetType();
                PipelineFilterAttribute[] selectors = type.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();

                List<MethodNode<T>> nodes = new();
                foreach (MethodInfo method in type.GetMethods())
                {
                    if (method.ReturnType != typeof(Task<T>))
                        continue;

                    PipelineFilterAttribute[] methodSelectors = method.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();
                    if (methodSelectors.Any())
                    {
                        MethodNode<T> node = factory.CreateNode<T>(provider, method, selectors.Concat(methodSelectors).ToArray());
                        nodes.Add(node);
                    }
                }
                groups.Add(new ClassNode<T>(nodes));
            }
            return new PipelineGraph<T>(groups);
        }
    }
}