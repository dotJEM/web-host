using System.Collections.Generic;
using System.Linq;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class PipelineContextFactory : IPipelineContextFactory
    {
        public PipelineContext Create(string caller, string contentType, JObject json)
        {
            //Note: Default pipeline doesn't require any initialization.
            return new PipelineContext();
        }
    }

    public class Pipeline : IPipeline
    {
        private const string PIPELINE = "pipeline";
        public IPipelineContextFactory ContextFactory { get; }

        private readonly IEnumerable<IPipelineHandler> handlers;
        private readonly ILogger performance;

        public Pipeline(IPipelineHandlerSet handlers, ILogger performance)
            : this(handlers, performance, new PipelineContextFactory())
        {
        }

        public Pipeline(IPipelineHandlerSet handlers, ILogger performance, IPipelineContextFactory contextFactory)
        {
            ContextFactory = contextFactory;
            this.handlers = handlers;
            this.performance = performance;
        }
        
        public JObject ExecuteAfterGet(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.AfterGet(jo, contentType, context), PIPELINE, new {
                        contentType,
                        method = "AfterGet", 
                        step = step.GetType().FullName
                    }));
        }

        public JObject ExecuteBeforePost(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.BeforePost(jo, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "BeforePost",
                        step = step.GetType().FullName
                    })
                );

        }

        public JObject ExecuteAfterPost(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.AfterPost(jo, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "AfterPost",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteBeforePut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.BeforePut(jo, prev, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "BeforePut",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteAfterPut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.AfterPut(jo, prev, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "AfterPut",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteBeforeRevert(JObject json, JObject current, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.BeforeRevert(jo, current, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "AfterPut",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteAfterRevert(JObject json, JObject current, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.AfterRevert(jo, current, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "AfterPut",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteBeforeDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.BeforeDelete(jo, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "BeforeDelete",
                        step = step.GetType().FullName
                    })
                );
        }

        public JObject ExecuteAfterDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction(() => step.AfterDelete(jo, contentType, context), PIPELINE, new
                    {
                        contentType,
                        method = "AfterDelete",
                        step = step.GetType().FullName
                    })
                );
        }
    }
}