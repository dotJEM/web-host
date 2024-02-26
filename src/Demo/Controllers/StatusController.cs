using System.Threading.Tasks;
using System.Web.Http;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Initialization;
using Newtonsoft.Json.Linq;

namespace Demo.Controllers
{
    [AllowAnonymous]
    public class StatusController : WebHostApiController
    {
        private readonly IInitializationTracker tracker;

        public StatusController(IInitializationTracker tracker)
        {
            this.tracker = tracker;
        }

        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult Get()
        {
            if (tracker.Completed)
            {
                return Ok(JObject.FromObject(new
                {
                    schemaVersion = "v1.0.0",
                    fileVersion = "v1.0.0",
                    version = "v1.0.0",
                }));
            }
            return ServiceUnavailable(JObject.FromObject(new
            {
                state = tracker,
                schemaVersion = "v1.0.0",
                fileVersion = "v1.0.0",
                version = "v1.0.0",
            }));
        }

    }
}