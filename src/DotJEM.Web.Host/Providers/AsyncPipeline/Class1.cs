using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{



    public class AsyncPipelineInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IPipelines>().ImplementedBy<JsonPipelineManager>().LifestyleTransient());
        }
    }


    /* NEW CONCEPT: Named pipelines */


    //Task<JObject> Execute<TContext>(TContext context, Func<TContext, JObject> finalize) where TContext : IJsonPipelineContext;


    public interface IPipelineMethod
    {
        PipelineExecutorDelegate Target { get; }
        NextFactoryDelegate NextFactory { get; }
        string Signature { get; }
    }

    public class MethodNode : IPipelineMethod
    {
        private readonly PipelineFilterAttribute[] filters;
        public string Signature { get; }

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; }

        public MethodNode(PipelineFilterAttribute[] filters, PipelineExecutorDelegate target, NextFactoryDelegate nextFactory, string signature)
        {
            this.filters = filters;
            this.Signature = signature;
            this.Target = target;
            NextFactory = nextFactory;
        }

        public bool Accepts(IJsonPipelineContext context)
        {
            return filters.All(selector => selector.Accepts(context));
        }
    }

    public class ClassNode
    {
        private readonly List<MethodNode> nodes;

        public ClassNode(List<MethodNode> nodes)
        {
            this.nodes = nodes;
        }

        public IEnumerable<MethodNode> For(IJsonPipelineContext context)
        {
            return nodes.Where(n => n.Accepts(context));
        }
    }

    public delegate Task<JObject> PipelineExecutorDelegate(IJsonPipelineContext context, INext next);

    public delegate INext NextFactoryDelegate(IJsonPipelineContext context, INode node);

    public interface IPipelines
    {
        ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext;
    }

    public static class EnumerableExtensions
    {
        public static void Enumerate<T>(this IEnumerable<T> source)
        {
            // ReSharper disable EmptyEmbeddedStatement
            foreach (T _ in source);
            // ReSharper restore EmptyEmbeddedStatement
        }
    }

    public class JsonPipelineManager : IPipelines
    {
        private readonly ILogger performance;
        private readonly List<ClassNode> nodes;
        private readonly ConcurrentDictionary<string, object> cache = new();
        private readonly Func<IJsonPipelineContext, string> keyGenerator;

        //TODO: Only relevant if performance is enabled, so the "strategy devide" needs to be pulled up.
        private readonly Func<IJsonPipelineContext, JObject> perfGenerator;

        public JsonPipelineManager(ILogger performance, IJsonPipelineHandler[] providers)
        {
            this.performance = performance;
            this.nodes = new PipelineGraphFactory().BuildHandlerGraph(providers);

            //TODO: Make key generator, by running over all nodes and seeing which properties they interact with, we know which properties to use to generate our key.
            //      We need to make sure we pass all the nodes to ensure everything has been accounted for.

            SpyingContext context = new ();
            nodes.SelectMany(n => n.For(context)).Enumerate();
            keyGenerator = context.CreateKeyGenerator();
            perfGenerator = context.CreatePerfGenerator();
        }


        public ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext
        {
            return (ICompiledPipeline<TContext>)cache.GetOrAdd(keyGenerator(context), key =>
            {
                IEnumerable<MethodNode> matchingNodes = nodes.SelectMany(n => n.For(context));
                return new CompiledPipeline<TContext>(performance, perfGenerator, matchingNodes, final);
            });
        }

        private class SpyingContext : IJsonPipelineContext
        {
            private readonly HashSet<string> parameters = new();
            private readonly SHA256CryptoServiceProvider provider = new ();
            private readonly Encoding encoding = Encoding.UTF8;


            public bool TryGetValue(string key, out string value)
            {
                parameters.Add(key);
                value = "";
                return true;
            }
            public Func<IJsonPipelineContext, string> CreateKeyGenerator()
            {
                Console.WriteLine(string.Join(", ", parameters));
                return context =>
                {
                    IEnumerable<byte> bytes = parameters.SelectMany(key => context.TryGetValue(key, out string value) ? encoding.GetBytes(value) : Array.Empty<byte>());
                    byte[] hash = provider.ComputeHash(bytes.ToArray());
                    return string.Join("", hash.Select(b => b.ToString("X2")));
                };
            }
            public Func<IJsonPipelineContext, JObject> CreatePerfGenerator()
            {
                return context =>
                {
                    return parameters.Aggregate(new JObject(), (obj, key) =>
                    {
                        if (context.TryGetValue(key, out string value))
                            obj[key] = value;
                        return obj;
                    });
                };
            }

            public object GetParameter(string key)
            {
                throw new NotImplementedException();
            }

            public IJsonPipelineContext Replace(params (string key, object value)[] values)
            {
                throw new NotImplementedException();
            }

        }
    }

    public interface ICompiledPipeline<in TContext> where TContext : IJsonPipelineContext
    {
        Task<JObject> Invoke(TContext context);
    }
    public class TerminationMethod : IPipelineMethod
    {
        public TerminationMethod(PipelineExecutorDelegate target)
        {
            Target = target;
        }

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; } = (_, _) => null;
        public string Signature => "PipelineTermination";
    }

    public class CompiledPipeline<TContext> : ICompiledPipeline<TContext> where TContext : IJsonPipelineContext
    {
        private readonly INode target;


        public CompiledPipeline(ILogger performance, Func<IJsonPipelineContext, JObject> perfGenerator, IEnumerable<MethodNode> nodes, Func<TContext, Task<JObject>> final)
        {
            if (performance.IsEnabled())
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new PNode(performance,perfGenerator, new TerminationMethod((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new PNode(performance,perfGenerator, methodNode, node));
            }
            else
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new Node(new TerminationMethod((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new Node(methodNode, node));
            }
        }

        public Task<JObject> Invoke(TContext context)
        {
            return target.Invoke(context);
        }

        private class PNode : INode
        {
            private readonly INode next;
            private readonly PipelineExecutorDelegate target;
            private readonly NextFactoryDelegate factory;
            private readonly ILogger performance;
            private readonly Func<IJsonPipelineContext, JObject> perfGenerator;

            public PNode(ILogger performance, Func<IJsonPipelineContext, JObject> perfGenerator, IPipelineMethod method, INode next)
            {
                this.performance = performance;
                this.perfGenerator = perfGenerator;
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
            }

            public async Task<JObject> Invoke(IJsonPipelineContext context)
            {
                //TODO: Here we generate the same JObject again and again, however it may be faster than reusing and clearing correctly.
                JObject info = perfGenerator(context);
                info["$$handler"] = "";
                using (performance.Track("pipeline", info))
                    return await target(context, factory(context, next));
            }

            //private IDisposable Track(IContext context, [CallerMemberName] string method = null)
            //{
            //    return performance.Track("pipeline", CreateMessage(context.ContentType, method));
            //}

            //private JToken CreateMessage(string contentType, string method)
            //{
            //    return new JObject
            //    {
            //        ["target"] = targetName,
            //        ["method"] = method,
            //        ["contentType"] = contentType
            //    };
            //}
        }

        private class Node : INode
        {
            private readonly INode next;
            private readonly PipelineExecutorDelegate target;
            private readonly NextFactoryDelegate factory;

            public Node(IPipelineMethod method, INode next)
            {
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
            }

            public Task<JObject> Invoke(IJsonPipelineContext context)
            {
                return target(context, factory(context, next));
            }
        }
    }

    public interface INode
    {
        Task<JObject> Invoke(IJsonPipelineContext context);
    }

    public interface INext
    {
        Task<JObject> Invoke();
    }

    public class Next : INext
    {
        protected INode NextNode { get; }
        protected IJsonPipelineContext Context { get; }

        public Next(IJsonPipelineContext context, INode next)
        {
            this.Context = context;
            this.NextNode = next;
        }

        public Task<JObject> Invoke() => NextNode.Invoke(Context);
    }
    public interface INext<in T> : INext
    {
        Task<JObject> Invoke(T arg);
    }

    public class Next<T> : Next, INext<T>
    {
        private readonly string parameterName;

        public Next(IJsonPipelineContext context, INode next, string parameterName)
            : base(context, next)
        {
            this.parameterName = parameterName;
        }

        public Task<JObject> Invoke(T arg) => NextNode.Invoke(Context.Replace((parameterName, arg)));
    }

    public interface INext<in T1, in T2> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2);
    }

    public class Next<T1, T2> : Next, INext<T1, T2>
    {
        private readonly string arg1Name;
        private readonly string arg2Name;

        public Next(IJsonPipelineContext context, INode next, string arg1Name, string arg2Name)
            : base(context, next)
        {
            this.arg1Name = arg1Name;
            this.arg2Name = arg2Name;
        }

        public Task<JObject> Invoke(T1 arg1, T2 arg2) => NextNode.Invoke(Context
            .Replace((arg1Name, arg1), (arg2Name, arg2)));
    }

    public interface INext<in T1, in T2, in T3> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3);
    }

    public interface INext<in T1, in T2, in T3, in T4> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    public interface INext<in T1, in T2, in T3, in T4, in T5> : INext
    {
        Task<JObject> Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }


    public interface IJsonPipelineHandler
    {
    }

    public interface IJsonPipeline
    {
        Task<JObject> Execute<TContext>(TContext context, Func<TContext, JObject> finalize) where TContext : IJsonPipelineContext;
    }

    public interface IJsonPipelineContext
    {
        bool TryGetValue(string key, out string value);

        object GetParameter(string key);

        IJsonPipelineContext Replace(params (string key, object value)[] values);
    }


    public abstract class PipelineFilterAttribute : Attribute
    {
        public abstract bool Accepts(IJsonPipelineContext context);
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PropertyFilterAttribute : PipelineFilterAttribute
    {
        private readonly string key;
        private readonly Regex filter;

        public PropertyFilterAttribute(string key, string regex, RegexOptions options = RegexOptions.None)
        {
            this.key = key;
            //NOTE: Force compiled.
            filter = new Regex(regex, options | RegexOptions.Compiled);
        }

        public override bool Accepts(IJsonPipelineContext context)
        {
            return context.TryGetValue(key, out string value) && filter.IsMatch(value);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : PropertyFilterAttribute
    {

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
        : base("contentType", regex, options)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpMethodFilterAttribute : PropertyFilterAttribute
    {

        public HttpMethodFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
            : base("method", regex, options | RegexOptions.IgnoreCase)
        {
        }
    }



    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PipelineDepencency : Attribute
    {
        public Type Type { get; set; }

        public PipelineDepencency(Type other)
        {
            Type = other;
        }

        public static PipelineDepencency[] GetDepencencies(object handler)
        {
            Type type = handler.GetType();
            return type
                .GetCustomAttributes(typeof(PipelineDepencency), true)
                .OfType<PipelineDepencency>()
                .ToArray();
        }
    }
}
