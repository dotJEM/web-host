using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public interface IPipelineHandler
    {
        bool Accept(string contentType);

        JObject BeforeGet(dynamic entity, string contentType, PipelineContext context);
        JObject AfterGet(dynamic entity, string contentType, PipelineContext context);
        JObject BeforePost(dynamic entity, string contentType, PipelineContext context);
        JObject AfterPost(dynamic entity, string contentType, PipelineContext context);
        JObject BeforeDelete(dynamic entity, string contentType, PipelineContext context);
        JObject AfterDelete(dynamic entity, string contentType, PipelineContext context);
        JObject BeforePut(dynamic entity, dynamic previous, string contentType, PipelineContext context);
        JObject AfterPut(dynamic entity, dynamic previous, string contentType, PipelineContext context);
    }
}