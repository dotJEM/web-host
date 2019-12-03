using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Results
{
    public class ServiceUnavailableMessageResultWithJson : NegotiatedContentResult<JObject>
    {
        public ServiceUnavailableMessageResultWithJson(HttpStatusCode statusCode, JObject content, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters) : base(statusCode, content, contentNegotiator, request, formatters)
        {
        }

        public ServiceUnavailableMessageResultWithJson(HttpStatusCode statusCode, JObject content, ApiController controller) : base(statusCode, content, controller)
        {
        }
    }

    public class ServiceUnavailableMessageResult : NegotiatedContentResult<string>
    {
        public ServiceUnavailableMessageResult(HttpStatusCode statusCode, string message, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
            : base(statusCode, message, contentNegotiator, request, formatters)
        {
        }

        public ServiceUnavailableMessageResult(HttpStatusCode statusCode, string message, ApiController controller)
            : base(statusCode, message, controller)
        {
        }
    }
}