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

        public JObject ExecuteBeforeGet(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforeGet(jo, contentType));
        }

        public JObject ExecuteAfterGet(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterGet(jo, contentType));
        }

        public JObject ExecuteBeforePost(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforePost(jo, contentType));
        }

        public JObject ExecuteAfterPost(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterPost(jo, contentType));
        }

        public JObject ExecuteBeforePut(JObject json, JObject prev, string contentType)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => step.BeforePut(jo, prev, contentType));
        }

        public JObject ExecuteAfterPut(JObject json, JObject prev, string contentType)
        {
            return handlers
                .Where(step => step.Accept(contentType))
                .Aggregate(json, (jo, step) => step.AfterPut(jo, prev, contentType));
        }

        public JObject ExecuteBeforeDelete(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.BeforeDelete(jo, contentType));
        }

        public JObject ExecuteAfterDelete(JObject json, string contentType)
        {
            return handlers.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.AfterDelete(jo, contentType));
        }
    }
}