using System.Net;
using System.Web.Http;
using DotJEM.Web.Host.ActionResults;

namespace DotJEM.Web.Host
{
    public abstract class WebHostApiController : ApiController
    {
        protected virtual NotFoundErrorMessageResult NotFound(string message)
        {
            return new NotFoundErrorMessageResult(HttpStatusCode.NotFound, message, this);
        }
    }
}