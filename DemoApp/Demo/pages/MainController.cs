using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using DotJEM.Web.Host;

namespace Demo.pages
{
    public class MainController : Controller
    {
        public ActionResult Get()
        {
            if (WebHost.Initialization.Completed)
                return View("~/Pages/Index.cshtml");
            return View("~/Pages/Index.cshtml");
        }
    }
}