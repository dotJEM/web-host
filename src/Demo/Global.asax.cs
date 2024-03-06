using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using DotJEM.Json.Index2.Management;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost(GlobalConfiguration.Configuration).Start();
        }
    }
}
