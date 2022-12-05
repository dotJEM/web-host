using System.Net;
using System.Web.Http;
using DotJEM.Web.Host.Results;

namespace DotJEM.Web.Host;

public static class ApiControllerExtentions
{
    public static NotFoundErrorMessageResult NotFound(this ApiController self, string message)
    {
        return new NotFoundErrorMessageResult(HttpStatusCode.NotFound, message, self);
    }
}