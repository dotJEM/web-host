using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public interface IPipelineContextFactory
    {
        PipelineContext Create(string caller, string contentType, JObject json);
    }

    public interface IPipeline
    {
        IPipelineContextFactory ContextFactory { get; }

        JObject ExecuteAfterGet(JObject json, string contentType, PipelineContext context);

        JObject ExecuteBeforePost(JObject json, string contentType, PipelineContext context);
        JObject ExecuteAfterPost(JObject json, string contentType, PipelineContext context);

        JObject ExecuteBeforePut(JObject json, JObject prev, string contentType, PipelineContext context);
        JObject ExecuteAfterPut(JObject json, JObject prev, string contentType, PipelineContext context);

        JObject ExecuteBeforeDelete(JObject json, string contentType, PipelineContext context);
        JObject ExecuteAfterDelete(JObject json, string contentType, PipelineContext context);
        JObject ExecuteBeforeRevert(JObject json, JObject current, string contentType, PipelineContext context);
        JObject ExecuteAfterRevert(JObject json, JObject current, string contentType, PipelineContext context);
    }
}