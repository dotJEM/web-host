using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;

namespace DotJEM.Web.Host.Diagnostics
{
    public class WebHostExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            HttpResponseMessage message = context.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, context.Exception);
            context.Result = new ExceptionMessageResult(message);
        }
    }

    public class ExceptionMessageResult : IHttpActionResult
    {
        private readonly HttpResponseMessage message;

        public ExceptionMessageResult(HttpResponseMessage message)
        {
            this.message = message;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(message);
        }
    }

}
