using System.Web.Mvc;

namespace Demo.Controllers
{
    public class IndexController : Controller
    {
        public ActionResult Get()
        {
                return View("~/Index.cshtml");
        }
    }
}
