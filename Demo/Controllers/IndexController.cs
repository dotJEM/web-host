using System.Web.Http;
using System.Web.Mvc;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace Demo.Controllers
{
    public class IndexController : Controller
    {
        public ActionResult Get()
        {
                return View("~/Index.cshtml");
        }
    }

    public class ExceptionController : ApiController
    {
        public string Get()
        {
            throw new JsonMergeConflictException(new MergeResult(true, new JObject(), new JObject(), new JObject(), new JObject()));
        }
    }
}
