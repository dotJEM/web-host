using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class Pipeline : IPipeline
    {
        private readonly IEnumerable<IPipelineHandler> handlers;

        public Pipeline(IPipelineHandlerSet handlers)
        {
            this.handlers = handlers;
        }

        public JObject ExecuteBeforeGet(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforeGet(jo, contentType, context));
        }

        public JObject ExecuteAfterGet(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterGet(jo, contentType, context));
        }

        public JObject ExecuteBeforePost(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforePost(jo, contentType, context));
        }

        public JObject ExecuteAfterPost(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterPost(jo, contentType, context));
        }

        public JObject ExecuteBeforePut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => step.BeforePut(jo, prev, contentType, context));
        }

        public JObject ExecuteAfterPut(JObject json, JObject prev, string contentType, PipelineContext context)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => step.AfterPut(jo, prev, contentType, context));
        }

        public JObject ExecuteBeforeDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforeDelete(jo, contentType, context));
        }

        public JObject ExecuteAfterDelete(JObject json, string contentType, PipelineContext context)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterDelete(jo, contentType, context));
        }
    }
}