using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Common;
using DotJEM.Web.Host.Providers.AsyncPipeline.Factories;
using Newtonsoft.Json.Linq;

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
            IList<IClassNode<T>> nodes = factory.GetHandlers<T>();
            SpyingContext spy = new();
            nodes.SelectMany(n => n.For(spy)).Enumerate();
            Func<IPipelineContext, string> keyGenerator = spy.CreateKeyGenerator();
            //TODO: Only relevant if performance is enabled, so the "strategy devide" needs to be pulled up.
            Func<IPipelineContext, JObject> perfGenerator = spy.CreatePerfGenerator();

            return (IUnboundPipeline<TContext, T>)cache.GetOrAdd(keyGenerator(context), key =>
            {
                IEnumerable<MethodNode<T>> matchingNodes = nodes.SelectMany(n => n.For(context));
                return new UnboundPipeline<TContext, T>(performance, perfGenerator, matchingNodes, final);
            });
        }

        private class SpyingContext : IPipelineContext
        {
            private readonly HashSet<string> parameters = new();
            private readonly SHA256CryptoServiceProvider provider = new ();
            private readonly Encoding encoding = Encoding.UTF8;

            public bool TryGetValue(string key, out object value)
            {
                parameters.Add(key);
                value = "";
                return true;
            }

            public Func<IPipelineContext, string> CreateKeyGenerator()
            {
                Console.WriteLine(string.Join(", ", parameters));
                return context =>
                {
                    IEnumerable<byte> bytes = parameters.SelectMany(key => context.TryGetValue(key, out object value) ? encoding.GetBytes(value.ToString()) : Array.Empty<byte>());
                    byte[] hash = provider.ComputeHash(bytes.ToArray());
                    return string.Join("", hash.Select(b => b.ToString("X2")));
                };
            }
            public Func<IPipelineContext, JObject> CreatePerfGenerator()
            {
                return context =>
                {
                    return parameters.Aggregate(new JObject(), (obj, key) =>
                    {
                        if (context.TryGetValue(key, out object value))
                            obj[key] = value.ToString();
                        return obj;
                    });
                };
            }

            public object this[string key]
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public object GetParameter(string key) => throw new NotImplementedException();
            public IPipelineContext Replace(params (string key, object value)[] values) => throw new NotImplementedException();
            public IPipelineContext Add(string key, object value)
            {
                throw new NotImplementedException();
            }

            public IPipelineContext Set(string key, object value)
            {
                throw new NotImplementedException();
            }
        }
    }
}