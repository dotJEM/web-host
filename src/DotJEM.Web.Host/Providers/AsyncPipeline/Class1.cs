using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers;
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
    }

    public class MethodNode : IPipelineMethod
    {
        private readonly PipelineFilterAttribute[] filters;

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; }

        public MethodNode(PipelineFilterAttribute[] filters, PipelineExecutorDelegate target, NextFactoryDelegate nextFactory)
        {
            this.filters = filters;
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

    public class PipelineGraphFactory
    {
        private IEnumerable<T> OrderHandlers<T>(T[] steps)
        {
            Queue<T> queue = new(steps);
            Dictionary<Type, T> map = steps.ToDictionary(h => h.GetType());
            var ordered = new HashSet<Type>();
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
            return ordered.Select(type => map[type]).ToArray();
        }

        public List<ClassNode> BuildHandlerGraph(IJsonPipelineHandler[] providers)
        {
            List<ClassNode> groups = new();
            foreach (IJsonPipelineHandler provider in OrderHandlers(providers))
            {
                Type type = provider.GetType();
                PipelineFilterAttribute[] selectors = type.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();

                List<MethodNode> nodes = new();
                foreach (MethodInfo method in type.GetMethods())
                {
                    PipelineFilterAttribute[] methodSelectors = method.GetCustomAttributes().OfType<PipelineFilterAttribute>().ToArray();
                    if (methodSelectors.Any())
                    {
                        MethodNode node = new PipelineExecutorDelegateFactory().CreateNode(provider, method, selectors.Concat(methodSelectors).ToArray());
                        nodes.Add(node);
                    }
                }
                groups.Add(new ClassNode(nodes));
            }
            return groups;
        }

    }

    public class PipelineExecutorDelegateFactory
    {
        private static readonly MethodInfo contextParameterGetter = typeof(IJsonPipelineContext).GetMethod("GetParameter");

        public MethodNode CreateNode(object target, MethodInfo method, PipelineFilterAttribute[] filters)
        {
            PipelineExecutorDelegate @delegate = CreateInvocator(target, method);
            NextFactoryDelegate nextFactory = CreateNextFactoryDelegate(method);

            return new MethodNode(filters, @delegate, nextFactory);
        }

        public PipelineExecutorDelegate CreateInvocator(object target, MethodInfo method)
        {
            Expression<PipelineExecutorDelegate> lambda = BuildLambda(target, method);
            return lambda.Compile();
        }

        public Expression<PipelineExecutorDelegate> BuildLambda(object target, MethodInfo method)
        {
            ConstantExpression targetParameter = Expression.Constant(target);
            ParameterExpression contextParameter = Expression.Parameter(typeof(IJsonPipelineContext), "context");
            ParameterExpression nextParameter = Expression.Parameter(typeof(INext), "next");

            // context.GetParameter("first"), ..., context, (INextHandler<...>) next);
            List<Expression> parameters = BuildParameterList(method, contextParameter, nextParameter);
            UnaryExpression convertTarget = Expression.Convert(targetParameter, target.GetType());
            MethodCallExpression methodCall = Expression.Call(convertTarget, method, parameters);
            UnaryExpression castMethodCall = Expression.Convert(methodCall, typeof(Task<JObject>));
            return Expression.Lambda<PipelineExecutorDelegate>(castMethodCall, contextParameter, nextParameter);
        }

        private List<Expression> BuildParameterList(MethodInfo method, Expression contextParameter, Expression nextParameter)
        {
            // Validate that method's signature ends with Context and Next.
            ParameterInfo[] list = method.GetParameters();
            ParameterInfo contextParameterInfo = list[list.Length - 2];

            if (contextParameterInfo.ParameterType != typeof(IJsonPipelineContext))
                contextParameter = Expression.Convert(contextParameter, contextParameterInfo.ParameterType);

            return list
                .Take(list.Length - 2)
                .Select(info =>
                {
                    // context.GetParameter("name");
                    MethodCallExpression call = Expression.Call(contextParameter, contextParameterGetter, Expression.Constant(info.Name));

                    // (parameterType) context.GetParameter("name"); 
                    return (Expression)Expression.Convert(call, info.ParameterType);
                })
                .Append(contextParameter)
                .Append(Expression.Convert(nextParameter, list.Last().ParameterType))
                .ToList();
        }



        public NextFactoryDelegate CreateNextFactoryDelegate(MethodInfo method)
        {
            Expression<NextFactoryDelegate> lambda = CreateNextStuff(method);
            return lambda.Compile();
        }

        public Expression<NextFactoryDelegate> CreateNextStuff(MethodInfo method)
        {
            ParameterInfo[] list = method.GetParameters();
            ParameterInfo nextParameterInfo = list[list.Length - 1];
            Type[] generics = nextParameterInfo.ParameterType.GetGenericArguments();

            ParameterExpression contextParameter = Expression.Parameter(typeof(IJsonPipelineContext), "context");
            ParameterExpression nodeParameter = Expression.Parameter(typeof(INode), "node");

            Expression[] arguments = list
                .Take(list.Length - 2)
                .Select(p => (Expression)Expression.Constant(p.Name))
                .Prepend(nodeParameter)
                .Prepend(contextParameter)
                .ToArray();
            MethodCallExpression methodCall = Expression.Call(typeof(NextFactory), nameof(NextFactory.Create), generics, arguments);

            return Expression.Lambda<NextFactoryDelegate>(methodCall, contextParameter, nodeParameter);
        }

        public static class NextFactory
        {
            public static INext<T> Create<T>(IJsonPipelineContext context, INode next, string paramName)
                => new Next<T>(context, next, paramName);
            public static INext<T1, T2> Create<T1, T2>(IJsonPipelineContext context, INode next, string paramName1, string paramName2)
                => new Next<T1, T2>(context, next, paramName1, paramName2);
        }
    }
    
    public interface IPipelines
    {
        ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext;
    }

    public class JsonPipelineManager : IPipelines
    {
        private readonly ILogger performance;
        private readonly List<ClassNode> nodes;
        private readonly ConcurrentDictionary<string, object> cache = new();
        private readonly Func<IJsonPipelineContext, string> keyGenerator;
        public JsonPipelineManager(ILogger performance, IJsonPipelineHandler[] providers)
        {
            this.performance = performance;
            this.nodes = new PipelineGraphFactory().BuildHandlerGraph(providers);

            //TODO: Make key generator, by running over all nodes and seeing which properties they interact with, we know which properties to use to generate our key.
            //      We need to make sure we pass all the nodes to ensure everything has been accounted for.

            var context = new SpyingContext();
            nodes.SelectMany(n => n.For(context)).ToArray();
            keyGenerator = context.CreateKeyGenerator();
        }

        
        public ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext
        {
            //TODO: Spying context is a one-time execution, ones it has been executed one time, we know all the
            //      properties that can influence the selection, from there we can make a key generator and then use that.
            //      - This also means that we can create the key generator in the constructor.
            //var recording = new SpyingContext(context);
            string key = keyGenerator(context);
            Console.WriteLine(key);
            return (ICompiledPipeline<TContext>) cache.GetOrAdd(key, k =>
            {
                IEnumerable<MethodNode> matchingNodes = nodes.SelectMany(n => n.For(context));
            return new CompiledPipeline<TContext>(performance, matchingNodes, final);
            });
        }

        private class SpyingContext : IJsonPipelineContext
        {
            private readonly HashSet<string> parameters = new ();

            public bool TryGetValue(string key, out string value)
            {
                parameters.Add(key);
                value = "";
                return true;
            }
            public Func<IJsonPipelineContext, string> CreateKeyGenerator()
            {
                Console.WriteLine(string.Join(", ", parameters));
                Encoding encoding = Encoding.UTF8;
                SHA256CryptoServiceProvider provider = new SHA256CryptoServiceProvider();
                return context =>
                {
                    IEnumerable<byte> bytes = parameters.SelectMany(key => context.TryGetValue(key, out string value) ? encoding.GetBytes(value) : new byte[0]);
                    byte[] hash = provider.ComputeHash(bytes.ToArray());
                    return string.Join("", hash.Select(b => b.ToString("X2")));
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
    public class NullMethod : IPipelineMethod
    {
        public NullMethod(PipelineExecutorDelegate target)
        {
            Target = target;
        }

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; } = (_, _) => null;
    }

    public class CompiledPipeline<TContext> : ICompiledPipeline<TContext> where TContext : IJsonPipelineContext
    {
        private readonly INode target;
        

        public CompiledPipeline(ILogger performance, IEnumerable<MethodNode> nodes, Func<TContext, Task<JObject>> final)
        {
            if (performance.IsEnabled())
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new PNode(performance, new NullMethod((context, _) => final((TContext)context)), null), 
                        (node, methodNode) => new PNode(performance, methodNode, node));
            }
            else
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new Node(new NullMethod((context, _) => final((TContext)context)), null), 
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

            public PNode(ILogger performance, IPipelineMethod method, INode next)
            {
                this.performance = performance;
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
            }

            public async Task<JObject> Invoke(IJsonPipelineContext context)
            {
                using (performance.Track(""))
                {
                    return await target(context, factory(context, next));
                }
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
            : base("method", regex, options |RegexOptions.IgnoreCase)
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
