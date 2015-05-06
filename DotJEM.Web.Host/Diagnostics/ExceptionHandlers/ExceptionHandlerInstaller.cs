using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Validation;

namespace DotJEM.Web.Host.Diagnostics.ExceptionHandlers
{
    public class ExceptionHandlerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<HttpResponseExceptionHandler>());
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<JsonEntityValidationExceptionHandler>());
        }
    }

    public class HttpResponseExceptionHandler : GenericExceptionHandler<HttpResponseException>
    {
        protected override IHttpActionResult Handle(HttpResponseException exception, HttpRequestMessage request)
        {
            return new ResponseMessageResult(exception.Response);
        }
    }

    public class JsonEntityValidationExceptionHandler : GenericExceptionHandler<JsonEntityValidationException>
    {
        protected override IHttpActionResult Handle(JsonEntityValidationException exception, HttpRequestMessage request)
        {
            HttpResponseMessage message = request.CreateErrorResponse(HttpStatusCode.BadRequest,exception.Message, exception);
            return new ResponseMessageResult(message);
        }
    }
}