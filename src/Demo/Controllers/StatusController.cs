using System.Threading.Tasks;
using System.Web.Http;
using DotJEM.Web.Host;

namespace Demo.Controllers
{
    [AllowAnonymous]
    public class StatusController : WebHostApiController
    {
        [HttpGet]
        public async Task<object> Get()
        {
            return WebHost.Initialization;
        }

    }
}