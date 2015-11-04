using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Diagnostics.Performance;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class Pipeline : IPipeline
    {
        private readonly IEnumerable<IPipelineHandler> handlers;
        private readonly IPerformanceLogger performance;

        public Pipeline(IPipelineHandlerSet handlers, IPerformanceLogger performance)
        {
            this.handlers = handlers;
            this.performance = performance;
        }
        
        public JObject ExecuteAfterGet(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.AfterGet(jo, contentType, context), 
                    contentType, "AfterGet", step.GetType().FullName)
                );
        }

        public JObject ExecuteBeforePost(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.BeforePost(jo, contentType, context),
                    contentType, "BeforePost", step.GetType().FullName)
                );

        }

        public JObject ExecuteAfterPost(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.AfterPost(jo, contentType, context),
                    contentType, "AfterPost", step.GetType().FullName)
                );
        }

        public JObject ExecuteBeforePut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.BeforePut(jo, prev, contentType, context),
                    contentType, "BeforePut", step.GetType().FullName)
                );
        }

        public JObject ExecuteAfterPut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.AfterPut(jo, prev, contentType, context),
                    contentType, "AfterPut", step.GetType().FullName)
                );
        }

        public JObject ExecuteBeforeDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.BeforeDelete(jo, contentType, context),
                    contentType, "BeforeDelete", step.GetType().FullName)
                );
        }

        public JObject ExecuteAfterDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => performance
                    .TrackFunction("pipeline", () => step.AfterDelete(jo, contentType, context),
                    contentType, "AfterDelete", step.GetType().FullName)
                );
        }
    }
}