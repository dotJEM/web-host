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
        ICompiledPipeline<TContext, T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : IPipelineContext;
    }
    public class PipelineManager : IPipelines
    {
        private readonly ILogger performance;
        private readonly IPipelineHandler[] providers;
        //private readonly List<IClassNode<JObject>> nodes;

        private readonly ConcurrentDictionary<string, object> cache = new();
        //private readonly Func<IPipelineContext, string> keyGenerator;

        //TODO: Only relevant if performance is enabled, so the "strategy devide" needs to be pulled up.
        //private readonly Func<IPipelineContext, JObject> perfGenerator;

        public PipelineManager(ILogger performance, IPipelineHandler[] providers)
        {
            this.performance = performance;
            this.providers = providers;
            //this.nodes = new PipelineGraphFactory().BuildHandlerGraph<JObject>(providers);

            //TODO: Make key generator, by running over all nodes and seeing which properties they interact with, we know which properties to use to generate our key.
            //      We need to make sure we pass all the nodes to ensure everything has been accounted for.

            //SpyingContext context = new ();
           // nodes.SelectMany(n => n.For(context)).Enumerate();
           // keyGenerator = context.CreateKeyGenerator();
           // perfGenerator = context.CreatePerfGenerator();
        }

        public ICompiledPipeline<TContext, T> For<TContext, T>(TContext context, Func<TContext, Task<T>> final) where TContext : IPipelineContext
        {
            List<IClassNode<T>> nodes = new PipelineGraphFactory().BuildHandlerGraph<T>(providers);
            SpyingContext spy = new();
            nodes.SelectMany(n => n.For(context)).Enumerate();
            Func<IPipelineContext, string> keyGenerator = spy.CreateKeyGenerator();
            Func<IPipelineContext, JObject> perfGenerator = spy.CreatePerfGenerator();

            return (ICompiledPipeline<TContext, T>)cache.GetOrAdd(keyGenerator(context), key =>
            {
                IEnumerable<MethodNode<T>> matchingNodes = nodes.SelectMany(n => n.For(context));
                return new CompiledPipeline<TContext, T>(performance, perfGenerator, matchingNodes, final);
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

            public object GetParameter(string key)
            {
                throw new NotImplementedException();
            }

            public IPipelineContext Replace(params (string key, object value)[] values)
            {
                throw new NotImplementedException();
            }

        }
    }
}