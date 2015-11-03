using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public interface IPipelineHandler
    {
        bool Accept(string contentType);

        JObject BeforeGet(dynamic entity, string contentType);
        JObject AfterGet(dynamic entity, string contentType);
        JObject BeforePost(dynamic entity, string contentType);
        JObject AfterPost(dynamic entity, string contentType);
        JObject BeforeDelete(dynamic entity, string contentType);
        JObject AfterDelete(dynamic entity, string contentType);
        JObject BeforePut(dynamic entity, dynamic previous, string contentType, PipelineContext context);
        JObject AfterPut(dynamic entity, dynamic previous, string contentType, PipelineContext context);
    }
}