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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using DotJEM.Web.Host.Providers.AsyncPipeline.Handlers;
using DotJEM.Web.Host.Providers.Pipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{

    public interface IPipelineBuidler
    {
        IPipelineBuidler PushAfter(IAsyncPipelineHandler handler);
        IPipeline Build();
    }

    public class PipelineBuilder : IPipelineBuidler
    {
        private readonly IAsyncPipelineHandler handler;
        private readonly IPipelineBuidler next;

        public PipelineBuilder(IAsyncPipelineHandler handler, IPipelineBuidler next = null)
        {
            this.handler = handler;
            this.next = next;
        }

        public IPipelineBuidler PushAfter(IAsyncPipelineHandler handler) => new PipelineBuilder(handler, this);

        public IPipeline Build() => new Pipeline(handler, next.Build());
    }

    public class PerformanceTrackingPipelineBuilder : IPipelineBuidler
    {
        private readonly ILogger performance;
        private readonly IAsyncPipelineHandler handler;
        private readonly IPipelineBuidler next;

        public PerformanceTrackingPipelineBuilder(ILogger performance, IAsyncPipelineHandler handler, IPipelineBuidler next = null)
        {
            this.performance = performance;
            this.handler = handler;
            this.next = next;
        }

        public IPipelineBuidler PushAfter(IAsyncPipelineHandler handler) => new PerformanceTrackingPipelineBuilder(performance, handler, this);

        public IPipeline Build() => new PerformanceTrackingPipeline(performance, handler, next?.Build());
    }

    public interface IPipeline
    {
        Task<JObject> Get(Guid id, IGetContext context);
        Task<JObject> Post(JObject entity, IPostContext context);
        Task<JObject> Put(Guid id, JObject entity, IPutContext context);
        Task<JObject> Patch(Guid id, JObject entity, IPatchContext context);
        Task<JObject> Delete(Guid id, IDeleteContext context);
    }

    public class Pipeline : IPipeline
    {
        private readonly IPipeline next;
        private readonly IAsyncPipelineHandler handler;

        public Pipeline(IAsyncPipelineHandler handler, IPipeline next = null)
        {
            this.handler = handler;
            this.next = next;
        }

        public Task<JObject> Get(Guid id, IGetContext context)
        {
            return handler.Get(id, context, new NextHandler<Guid>(id, x => next.Get(x, context)));
        }

        public Task<JObject> Post(JObject entity, IPostContext context)
        {
            return handler.Post(entity, context, new NextHandler<JObject>(entity, x => next.Post(x, context)));
        }

        public Task<JObject> Put(Guid id, JObject entity, IPutContext context)
        {
            return handler.Put(id, entity, context, new NextHandler<Guid, JObject>(id, entity, (x, y) => next.Put(x, y, context)));
        }

        public Task<JObject> Patch(Guid id, JObject entity, IPatchContext context)
        {
            return handler.Patch(id, entity, context, new NextHandler<Guid, JObject>(id, entity, (x, y) => next.Patch(x, y, context)));
        }

        public Task<JObject> Delete(Guid id, IDeleteContext context)
        {
            return handler.Delete(id, context, new NextHandler<Guid>(id, x => next.Delete(x, context)));
        }
    }

    public class PerformanceTrackingPipeline : IPipeline
    {
        private readonly IPipeline next;
        private readonly ILogger performance;
        private readonly IAsyncPipelineHandler handler;

        private readonly string targetName;

        public PerformanceTrackingPipeline(ILogger performance, IAsyncPipelineHandler handler, IPipeline next = null)
        {
            this.performance = performance;
            this.handler = handler;
            this.next = next;
            this.targetName = handler.GetType().FullName;
        }
        
        public async Task<JObject> Get(Guid id, IGetContext context)
        {
            using (Track(context)) return await handler.Get(id, context, new NextHandler<Guid>(id, x => next.Get(x, context)));
        }

        public async Task<JObject> Post(JObject entity, IPostContext context)
        {
            using (Track(context)) return await handler.Post(entity, context, new NextHandler<JObject>(entity, x => next.Post(x, context)));
        }

        public async Task<JObject> Put(Guid id, JObject entity, IPutContext context)
        {
            using (Track(context)) return await handler.Put(id, entity, context, new NextHandler<Guid, JObject>(id, entity, (x, y) => next.Put(x, y, context)));
        }

        public async Task<JObject> Patch(Guid id, JObject entity, IPatchContext context)
        {
            using (Track(context)) return await handler.Patch(id, entity, context, new NextHandler<Guid, JObject>(id, entity, (x, y) => next.Patch(x, y, context)));
        }

        public async Task<JObject> Delete(Guid id, IDeleteContext context)
        {
            using (Track(context)) return await handler.Delete(id, context, new NextHandler<Guid>(id, x => next.Delete(x, context)));
        }

        private IDisposable Track(IContext context, [CallerMemberName] string method = null)
        {
            return performance.Track("pipeline", CreateMessage(context.ContentType, method));
        }

        private JToken CreateMessage(string contentType, string method)
        {
            return new JObject
            {
                ["target"] = targetName,
                ["method"] = method,
                ["contentType"] = contentType
            };
        }
    }

    public interface IAsyncPipelineContextFactory
    {
        IGetContext CreateGetContext(string contentType);
        IPostContext CreatePostContext(string contentType);
        IPutContext CreatePutContext(string contentType, JObject prevous);
        IPatchContext CreatePatchContext(string contentType, JObject prevous);
        IDeleteContext CreateDeleteContext(string contentType, JObject previous);
    }

    public class DefaultAsyncPipelineContextFactory : IAsyncPipelineContextFactory
    {
        public IGetContext CreateGetContext(string contentType) => new EmptyContext(contentType);
        public IPostContext CreatePostContext(string contentType) => new EmptyContext(contentType);
        public IPutContext CreatePutContext(string contentType, JObject previous) => new PreviousContext(contentType, previous);
        public IPatchContext CreatePatchContext(string contentType, JObject previous) => new PreviousContext(contentType, previous);
        public IDeleteContext CreateDeleteContext(string contentType, JObject previous) => new PreviousContext(contentType, previous);
    }

    public interface IAsyncPipelineFactory
    {
        IAsyncPipeline Create(IAsyncPipelineHandler termination);

    }

    public class AsyncPipelineFactory : IAsyncPipelineFactory
    {
        private readonly ILogger logger;
        private readonly IAsyncPipelineHandler[] handlers;
        private readonly IAsyncPipelineContextFactory contextFactory;

        public AsyncPipelineFactory(ILogger logger, IAsyncPipelineHandler[] handlers, IAsyncPipelineContextFactory contextFactory = null)
        {
            this.logger = logger;
            this.handlers = handlers;
            this.contextFactory = contextFactory ?? new DefaultAsyncPipelineContextFactory();
        }

        public IAsyncPipeline Create(IAsyncPipelineHandler termination)
        {
            return new AsyncPipeline(new AsyncPipelineHandlerCollection(logger, handlers, termination), contextFactory);
        }
    }

    public interface IAsyncPipeline
    {
        IAsyncPipelineContextFactory ContextFactory { get; }

        Task<JObject> Get(Guid id, IGetContext context);
        Task<JObject> Post(JObject entity, IPostContext context);
        Task<JObject> Put(Guid id, JObject entity, IPutContext context);
        Task<JObject> Patch(Guid id, JObject entity, IPatchContext context);
        Task<JObject> Delete(Guid id, IDeleteContext context);
    }

    public class AsyncPipeline : IAsyncPipeline
    {
        private readonly IAsyncPipelineHandlerCollection pipelines;

        public IAsyncPipelineContextFactory ContextFactory { get; }

        public AsyncPipeline(IAsyncPipelineHandlerCollection pipelines, IAsyncPipelineContextFactory factory)
        {
            ContextFactory = factory;
            this.pipelines = pipelines;
        }

        public Task<JObject> Get(Guid id, IGetContext context)
        {
            return pipelines.For(context.ContentType).Get(id, context);
        }

        public Task<JObject> Post(JObject entity, IPostContext context)
        {
            return pipelines.For(context.ContentType).Post(entity, context);
        }

        public Task<JObject> Put(Guid id, JObject entity, IPutContext context)
        {
            return pipelines.For(context.ContentType).Put(id, entity, context);
        }

        public Task<JObject> Patch(Guid id, JObject entity, IPatchContext context)
        {
            return pipelines.For(context.ContentType).Patch(id, entity, context);
        }

        public Task<JObject> Delete(Guid id, IDeleteContext context)
        {
            return pipelines.For(context.ContentType).Delete(id, context);
        }
    }

    public class AsyncPipelineInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IAsyncPipelineFactory>().ImplementedBy<AsyncPipelineFactory>().LifestyleTransient());
        }
    }




    /* NEW CONCEPT: Named pipelines */


    //Task<JObject> Execute<TContext>(TContext context, Func<TContext, JObject> finalize) where TContext : IJsonPipelineContext;
    
    [PropertyFilter("ContentType", ".*")]
    public class ExampleHandler : AsyncPipelineHandler
    {
        [PropertyFilter("Method", "GET", RegexOptions.IgnoreCase)]
        public override async Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next)
        {
            JObject entity = await next.Invoke().ConfigureAwait(false);
            entity["foo"] = "HAHA";
            return entity;
        }
    }

    public class MethodNode
    {
        private readonly PipelineFilterAttribute[] filters;

        public PipelineExecutorDelegate Target { get; }

        public MethodNode(PipelineFilterAttribute[] filters, PipelineExecutorDelegate target)
        {
            this.filters = filters;
            this.Target = target;
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

            return new MethodNode(filters, @delegate);
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

        private NextFactory factory = new NextFactory();
        private List<Expression> BuildParameterList(MethodInfo method, Expression contextParameter, Expression nextParameter)
        {
            // Validate that method's signature ends with Context and Next.
            ParameterInfo[] list = method.GetParameters();

            ParameterInfo nextParameterInfo = list[list.Length - 1];
            ParameterInfo contextParameterInfo = list[list.Length - 2];

            if (contextParameterInfo.ParameterType != typeof(IJsonPipelineContext))
                contextParameter = Expression.Convert(contextParameter, contextParameterInfo.ParameterType);

            if (nextParameterInfo.ParameterType.IsGenericType)
            {
                Type[] generics = nextParameterInfo.ParameterType.GetGenericArguments();
                //MethodInfo nextFacMethod = typeof(NextFactory)
                //    .GetMethods()
                //    .FirstOrDefault(m => m.Name == nameof(NextFactory.Create) && m.GetParameters().Length - 2 == genericTypes.Length);

                Expression[] arguments = generics
                    .Select(Expression.Parameter)
                    .Prepend(Expression.Parameter(typeof(INode)))
                    .Prepend(Expression.Parameter(typeof(IJsonPipelineContext)))
                    .ToArray();

                Expression factoryInstance = Expression.Constant(factory);
                MethodCallExpression factoryExp = Expression.Call(factoryInstance, "Create", generics, arguments);


                

            }

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

    }

    public class NextFactory
    {
        public INext<T> Create<T>(IJsonPipelineContext context, INode next, string paramName)
            => new Next<T>(context, next, paramName);
        public INext<T1, T2> Create<T1, T2>(IJsonPipelineContext context, INode next, string paramName1, string paramName2)
            => new Next<T1, T2>(context, next, paramName1, paramName2);
    }

    public interface IPipelines
    {
        ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext;
    }

    public class JsonPipelineManager : IPipelines
    {
        private readonly ILogger performance;
        private readonly List<ClassNode> nodes;
        private readonly ConcurrentDictionary<string, IPipeline> cache = new();

        public JsonPipelineManager(ILogger performance, IJsonPipelineHandler[] providers)
        {
            this.performance = performance;
            this.nodes = new PipelineGraphFactory().BuildHandlerGraph(providers);

            //TODO: Make key generator, by running over all nodes and seeing which properties they interact with, we know which properties to use to generate our key.
            //      We need to make sure we pass all the nodes to ensure everything has been accounted for.
        }

        
        //public Task<JObject> Invoke<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext;
        public ICompiledPipeline<TContext> For<TContext>(TContext context, Func<TContext, Task<JObject>> final) where TContext : IJsonPipelineContext
        {
            //TODO: Spying context is a one-time execution, ones it has been executed one time, we know all the
            //      properties that can influence the selection, from there we can make a key generator and then use that.
            //      - This also means that we can create the key generator in the constructor.
            var recording = new SpyingContext(context);
            var matchingNodes = nodes.SelectMany(n => n.For(context));

            return new CompiledPipeline<TContext>(matchingNodes, final);
        }

        private class SpyingContext : IJsonPipelineContext
        {
            private IJsonPipelineContext origin;

            public SpyingContext(IJsonPipelineContext origin)
            {
                this.origin = origin;
            }

            public bool TryGetValue(string key, out string value)
            {
                throw new NotImplementedException();
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

    public class CompiledPipeline<TContext> : ICompiledPipeline<TContext> where TContext : IJsonPipelineContext
    {
        private readonly INode target;
        
        public CompiledPipeline(IEnumerable<MethodNode> nodes, Func<TContext, Task<JObject>> final)
        {
            new Node((context, _) => final((TContext)context), null);

            this.target = nodes.Reverse()
                .Aggregate((INode)new Node((context, _) => final((TContext)context), null), (node, methodNode) => new Node(methodNode.Target, node));
        }

        public Task<JObject> Invoke(TContext context)
        {
            return target.Invoke(context);
        }

        private class Node : INode
        {
            private readonly INode next;
            private readonly PipelineExecutorDelegate target;

            public Node(PipelineExecutorDelegate target, INode next)
            {
                this.next = next;
                this.target = target;
            }

            public Task<JObject> Invoke(IJsonPipelineContext context)
            {
                return target(context, new Next(context, next));
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
    public class ContentTypeFilterAttribute : PipelineFilterAttribute
    {
        private readonly Regex filter;

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            filter = new Regex(regex, options);
        }

        public bool IsMatch(string contentType) => filter.IsMatch(contentType);

        public override bool Accepts(IJsonPipelineContext context)
        {
            return context.TryGetValue("contentType", out string value) && filter.IsMatch(value);
        }
    }

}
