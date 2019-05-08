using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost(GlobalConfiguration.Configuration).Start();
            GlobalConfiguration.Configuration.Services.Replace(typeof(IExceptionHandler), new MyExceptionHandler());
        }
    }

    public class MyExceptionHandler : System.Web.Http.ExceptionHandling.ExceptionHandler
    {
        public override Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                HttpResponseMessage message = context.Request.CreateResponse(HttpStatusCode.Conflict, context.Exception.GetType().FullName);
                context.Result = new ResponseMessageResult(message);
            }, cancellationToken);

        }
    }

    public class DemoHost : WebHost
    {
        public DemoHost(HttpConfiguration configuration) : base(configuration)
        {
        }

        protected override void Configure(IRouter router)
        {
            router.Route("api/exception").To<ExceptionController>();
            router.Default().To<IndexController>();
        }

        protected override void Configure(IWindsorContainer container)
        {
            container.Register(Component.For<ExceptionController>().LifestyleTransient());
        }
    }
}
