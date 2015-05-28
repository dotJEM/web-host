using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public interface IPipeline
    {
        JObject ExecuteAfterGet(JObject json, string contentType);

        JObject ExecuteBeforePost(JObject json, string contentType);
        JObject ExecuteAfterPost(JObject json, string contentType);

        JObject ExecuteBeforePut(JObject json, JObject prev, string contentType);
        JObject ExecuteAfterPut(JObject json, JObject prev, string contentType);

        JObject ExecuteBeforeDelete(JObject json, string contentType);
        JObject ExecuteAfterDelete(JObject json, string contentType);
    }
}