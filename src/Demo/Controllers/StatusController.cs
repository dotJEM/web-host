using System.Threading.Tasks;
using System.Web.Http;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Initialization;

namespace Demo.Controllers
{
    [AllowAnonymous]
    public class StatusController : WebHostApiController
    {
        [HttpGet]
        public Task<IInitializationTracker> Get()
        {
            return Task.FromResult(WebHost.Initialization);
        }

    }
}