using System.Web.Http;

namespace Demo.Controllers
{
    public class IdentityController : ApiController
    {
        public string Get()
        {
            return RequestContext.Principal?.Identity?.Name ?? "UNKNOWN";
        }
    }
}