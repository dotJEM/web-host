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
    public delegate Task<JObject> PipelineExecutorDelegate(IJsonPipelineContext context, INextHandler<Guid> next);
    
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
    public class JsonPipelineNode
    {
        public JsonPipelineNode(PipelineSelectorAttribute[] selectors, PipelineExecutorDelegate target)
        {
            
        }
    }
    public class JsonPipelineNodeGroup
    {
        public JsonPipelineNodeGroup(List<JsonPipelineNode> nodes)
        {
            
        }
    }


    public class JsonPipelineManager 
    {
        private readonly ILogger performance;
        private readonly List<JsonPipelineNodeGroup> nodes;
        private readonly ConcurrentDictionary<string, IPipeline> cache = new();

        public JsonPipelineManager(ILogger performance, IJsonPipelineHandler[] providers)
        {
            this.performance = performance;
            this.nodes = BuildHandlerGraph(providers);
        }

        private List<JsonPipelineNodeGroup> BuildHandlerGraph(IJsonPipelineHandler[] providers)
        {
            List<JsonPipelineNodeGroup> groups = new List<JsonPipelineNodeGroup>();
            foreach (IJsonPipeline provider in OrderHandlers(providers))
            {
                Type type = provider.GetType();
                PipelineSelectorAttribute[] selectors = type.GetCustomAttributes().OfType<PipelineSelectorAttribute>().ToArray();

                List<JsonPipelineNode> nodes = new List<JsonPipelineNode>();
                foreach (MethodInfo method in type.GetMethods())
                {
                    PipelineSelectorAttribute[] methodSelectors = method.GetCustomAttributes().OfType<PipelineSelectorAttribute>().ToArray();
                    if (methodSelectors.Any())
                    {
                        PipelineExecutorDelegate @delegate = Factory.CreateInvocator(provider, method);
                        nodes.Add(new JsonPipelineNode(selectors.Concat(methodSelectors).ToArray(), @delegate));
                    }
                }
                groups.Add(new JsonPipelineNodeGroup(nodes));
            }
            return groups;
        }

        private static IEnumerable<T> OrderHandlers<T>(T[] steps)
        {
            Queue<T> queue = new Queue<T>(steps);
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

        public IPipeline For(string contentType)
        {
            return cache.GetOrAdd(contentType, CreateBoundPipeline);
        }

        private IPipeline CreateBoundPipeline(string contentType)
        {
            //IPipelineBuidler pipeline = performance.IsEnabled()
            //    ? new PerformanceTrackingPipelineBuilder(performance, termination)
            //    : new PipelineBuilder(termination);
            //Stack<IAsyncPipelineHandler> filtered = new(handlers.Where(x => CheckHandler(x, contentType)));
            //while (filtered.Count > 0) pipeline = pipeline.PushAfter(filtered.Pop());
            //return pipeline.Build();
            throw new NotImplementedException();
        }
    }

    public class Factory
    {
        public static PipelineExecutorDelegate CreateInvocator(object target, MethodInfo method)
        {
            //JsonPipelineNode
            //TODO: Could we instead build a delegate which can extract parameters from the context there by mimimcing the original interface better?
            // E.g.
            /*
             * Task<JObject> Get(string contentType, Guid id, IContext context, INext next);
             *
             * ->   Execute((string)context.Get("contentType"), (Guid)context.Get("id"), context, next);
             */
            ParameterExpression targetParameter = Expression.Parameter(typeof(object), "target");
            ParameterExpression contextParameter = Expression.Parameter(typeof(IJsonPipelineContext), "context");
            ParameterExpression nextParameter = Expression.Parameter(typeof(INextHandler<>), "next");

            // https://github.com/dotJEM/aspnetcore-fluentrouting/blob/master/src/DotJEM.AspNetCore.FluentRouting/Invoker/Execution/LambdaExecutorDelegateFactory.cs
            foreach (ParameterInfo info in method.GetParameters())
            {

            }
            return (PipelineExecutorDelegate) method.CreateDelegate(typeof(PipelineExecutorDelegate), target);
        }

        private List<Expression> BuildParameterList(MethodInfo method, ParameterExpression source)
        {
            List<Expression> parameters = new List<Expression>();
            ParameterInfo[] infos = method.GetParameters();
            for (int i = 0; i < infos.Length; i++)
            {
                ParameterInfo info = infos[i];
                // arrayIndexAccessor: parameters[i]
                BinaryExpression arrayIndexAccessor = Expression.ArrayIndex(source, Expression.Constant(i));
                // castParameter: "(Ti) (FromBody<T..>) parameters[i]" or "(Ti) parameters[i]".
                UnaryExpression castParameter = CreateParameterCast(arrayIndexAccessor, info.ParameterType);
                parameters.Add(castParameter);
            }
            return parameters;
        }
        private UnaryExpression CreateParameterCast(BinaryExpression accessor, Type type)
        {
            //if (type.IsGenericType)
            //{
            //    Type firstInnerType = type.GenericTypeArguments.First();
            //    Type bindingSourceParameterType = typeof(BindingSourceParameter<>).MakeGenericType(firstInnerType);
            //    if (bindingSourceParameterType.IsAssignableFrom(type))
            //    {
            //        // castParameter: "(Ti) (FromBody<T..>) parameters[i]"
            //        return  Expression.Convert(Expression.Convert(accessor, firstInnerType), type);
            //    }
            //}
            // castParameter: "(Ti) parameters[i]"
            return Expression.Convert(accessor, type);
        }

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
    }

    public interface IPipelines
    {
        IJsonPipeline Select(IPipelineSelector selector);
    }

    public interface IPipelineSelector
    {
    }



    public interface INullSelector : IPipelineSelector { }

    public static class XPipeline
    {
        public static INullSelector For => null;
    }

    public static class PipelinesSelectorExtensions
    {
        public static IPipelineSelector ContentType(this IPipelineSelector self, string contentType)
        {
            return self.And(new ContentTypeSelector(contentType));
        }

        public static IPipelineSelector Name(this IPipelineSelector self, string name)
        {
            return self.And(new ContentTypeSelector(name));
        }

        public static IPipelineSelector And(this IPipelineSelector self, IPipelineSelector other)
        {
            if (self == null)
                return other;

            if (self is CompositeAndSelector and)
                return and.Merge(other);

            return new CompositeAndSelector(self, other);
        }
    }

    public class CompositeAndSelector : IPipelineSelector
    {
        private readonly IPipelineSelector[] selectors;

        public CompositeAndSelector(params IPipelineSelector[] selectors)
        {
            this.selectors = selectors;
        }

        public IPipelineSelector Merge(IPipelineSelector other)
        {
            if (other is CompositeAndSelector and)
                return new CompositeAndSelector(selectors.Concat(and.selectors).ToArray());
            return new CompositeAndSelector(selectors.Append(other).ToArray());
        }
    }

    public class ContentTypeSelector : IPipelineSelector
    {
        private readonly string contentType;

        public ContentTypeSelector(string contentType)
        {
            this.contentType = contentType;
        }
    }

    public class NameSelector : IPipelineSelector
    {
        private readonly string name;

        public NameSelector(string name)
        {
            this.name = name;
        }
    }


    public abstract class PipelineSelectorAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PropertyFilterAttribute : PipelineSelectorAttribute
    {
        private readonly string key;
        private readonly Regex filter;

        public PropertyFilterAttribute(string key, string regex, RegexOptions options = RegexOptions.None)
        {
            this.key = key;
            //NOTE: Force compiled.
            filter = new Regex(regex, options | RegexOptions.Compiled);
        }

        public bool Accepts(IJsonPipelineContext context)
        {
            return context.TryGetValue(key, out string value) && filter.IsMatch(value);
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : PipelineSelectorAttribute
    {
        private readonly Regex filter;

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            filter = new Regex(regex, options);
        }

        public bool IsMatch(string contentType) => filter.IsMatch(contentType);
    }

}
