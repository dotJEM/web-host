using System.Web;
using Demo.Controllers;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost().Start();
        }
    }

    public class DemoHost : WebHost
    {
        protected override void Configure(IRouter router)
        {
            router.Default().To<IndexController>();
        }
    }
}
