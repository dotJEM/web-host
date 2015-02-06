using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Results;

namespace DotJEM.Web.Host.ActionResults
{
    public class ForbiddenErrorMessageResult : NegotiatedContentResult<string>
    {
        public ForbiddenErrorMessageResult(HttpStatusCode statusCode, string message, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
            : base(statusCode, message, contentNegotiator, request, formatters)
        {
        }

        public ForbiddenErrorMessageResult(HttpStatusCode statusCode, string message, ApiController controller)
            : base(statusCode, message, controller)
        {
        }
    }
}