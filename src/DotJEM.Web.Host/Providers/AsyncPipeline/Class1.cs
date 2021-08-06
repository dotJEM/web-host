using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using DotJEM.Web.Host.Providers.Pipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public interface IContext
    {
        string ContentType { get; }
    }

    public interface IGetContext : IContext
    {
    }

    public interface IPostContext : IContext
    {
    }

    public interface IPutContext : IContext
    {
        JObject Previous { get; }
    }

    public interface IPatchContext : IContext
    {
        JObject Previous { get; }
    }

    public interface IDeleteContext : IContext
    {
        JObject Previous { get; }
    }

    internal class EmptyContext : IGetContext, IPostContext
    {
        public string ContentType { get; }
      
        public EmptyContext(string contentType)
        {
            ContentType = contentType;
        }
    }

    internal class PreviousContext : IPutContext, IPatchContext, IDeleteContext
    {
        public string ContentType { get; }
        public JObject Previous { get; }

        public PreviousContext(string contentType, JObject previous)
        {
            ContentType = contentType;
            Previous = previous;
        }
    }


    public interface IAsyncPipelineHandler
    {
        Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next);
        Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next);
        Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next);
        Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next);
        Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next);
    }

    public interface INextHandler<in TOptArg>
    {
        Task<JObject> Invoke();
        Task<JObject> Invoke(TOptArg narg);
    }

    public class NextHandler<TOptArg> : INextHandler<TOptArg>
    {
        private readonly TOptArg arg;
        private readonly Func<TOptArg, Task<JObject>> target;

        public NextHandler(TOptArg arg, Func<TOptArg, Task<JObject>> target)
        {
            this.arg = arg;
            this.target = target;
        }

        public Task<JObject> Invoke() => Invoke(arg);

        public Task<JObject> Invoke(TOptArg newArg) => target.Invoke(newArg);
    }

    public interface INextHandler<in TOptArg1, in TObtArg2>
    {
        Task<JObject> Invoke();
        Task<JObject> Invoke(TOptArg1 arg1, TObtArg2 arg2);
    }

    public class NextHandler<TOptArg1, TOptArg2> : INextHandler<TOptArg1, TOptArg2>
    {
        private readonly TOptArg1 arg1;
        private readonly TOptArg2 arg2;
        private readonly Func<TOptArg1, TOptArg2, Task<JObject>> target;

        public NextHandler(TOptArg1 arg1, TOptArg2 arg2, Func<TOptArg1, TOptArg2, Task<JObject>> target)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.target = target;
        }

        public Task<JObject> Invoke() => Invoke(arg1, arg2);

        public Task<JObject> Invoke(TOptArg1 newArg1, TOptArg2 newArg2) => target(newArg1, newArg2);
    }

    public abstract class AsyncPipelineHandler : IAsyncPipelineHandler
    {
        public virtual Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next) => next.Invoke();
        public virtual Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next) => next.Invoke();
        public virtual Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next) => next.Invoke();
        public virtual Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next) => next.Invoke(id, entity);
        public virtual Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next) => next.Invoke(id);
    }


    public interface IAsyncPipelineHandlerSet
    {
        IPipeline For(string contentType);
    }

    public class ContentTypeFilterAttribute : Attribute
    {
        private readonly Regex filter;

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            filter = new Regex(regex, options);
        }

        public bool IsMatch(string contentType) => filter.IsMatch(contentType);
    }

    public class AsyncPipelineHandlerSet : IAsyncPipelineHandlerSet
    {
        private readonly ILogger performance;
        private readonly IAsyncPipelineHandler termination;
        private readonly IAsyncPipelineHandler[] handlers;
        private readonly ConcurrentDictionary<string, IPipeline> cache = new ();

        public AsyncPipelineHandlerSet(ILogger performance, IAsyncPipelineHandler[] steps, IAsyncPipelineHandler termination)
        {
            this.performance = performance;
            this.termination = termination;
            handlers = OrderHandlers(steps);
        }
        
        public IPipeline For(string contentType)
        {
            return cache.GetOrAdd(contentType, CreateBoundPipeline);
        }

        private IPipeline CreateBoundPipeline(string contentType)
        {
            IPipelineBuidler pipeline = performance.IsEnabled()
                ? new PerformanceTrackingPipelineBuilder(performance, termination)
                : new PipelineBuilder(termination);
            Stack<IAsyncPipelineHandler> filtered = new Stack<IAsyncPipelineHandler>(handlers.Where(x => CheckHandler(x, contentType)));
            while (filtered.Count > 0) pipeline = pipeline.PushAfter(filtered.Pop());
            return pipeline.Build();
        }

        private bool CheckHandler(IAsyncPipelineHandler arg, string contentType)
        {
            //TODO: Enforce at least one Attribute.
            return arg
                .GetType()
                .GetCustomAttributes(typeof(ContentTypeFilterAttribute), true)
                .Cast<ContentTypeFilterAttribute>()
                .Any(att => att.IsMatch(contentType));
        }

        private static IAsyncPipelineHandler[] OrderHandlers(IAsyncPipelineHandler[] steps)
        {
            Queue<IAsyncPipelineHandler> queue = new Queue<IAsyncPipelineHandler>(steps);
            Dictionary<Type, IAsyncPipelineHandler> map = steps.ToDictionary(h => h.GetType());
            var ordered = new HashSet<Type>();
            while (queue.Count > 0)
            {
                IAsyncPipelineHandler handler = queue.Dequeue();
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
    }

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
            return new AsyncPipeline(new AsyncPipelineHandlerSet(logger, handlers, termination), contextFactory);
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
        private readonly IAsyncPipelineHandlerSet pipelines;

        public IAsyncPipelineContextFactory ContextFactory { get; }

        public AsyncPipeline(IAsyncPipelineHandlerSet pipelines, IAsyncPipelineContextFactory factory)
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
}
