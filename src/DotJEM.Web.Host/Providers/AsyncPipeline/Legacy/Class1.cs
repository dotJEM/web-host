using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using DotJEM.Web.Host.Providers.AsyncPipeline.Handlers;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Legacy
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

}
