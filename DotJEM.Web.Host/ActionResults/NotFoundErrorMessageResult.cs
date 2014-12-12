using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace DotJEM.Web.Host.ActionResults
{
    public class NotFoundErrorMessageResult : NegotiatedContentResult<string>
    {
        public NotFoundErrorMessageResult(HttpStatusCode statusCode, string message, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
            : base(statusCode, message, contentNegotiator, request, formatters)
        {
        }

        public NotFoundErrorMessageResult(HttpStatusCode statusCode, string message, ApiController controller)
            : base(statusCode, message, controller)
        {
        }
    }
}
