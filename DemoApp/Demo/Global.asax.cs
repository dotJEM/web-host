using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Demo.pages;
using Demo.Server;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;

namespace Demo
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost<MainController>().Start();
        }


    }

}
