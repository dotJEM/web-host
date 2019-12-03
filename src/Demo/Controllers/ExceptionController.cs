using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Results;

namespace Demo.Controllers
{
    public class ExceptionController : ApiController
    {
        public string Get()
        {
            throw new MyCustomException();
        }
    }

    public class MyCustomException : Exception
    {
    }

    public class MyCustomExceptionHandler : GenericExceptionHandler<MyCustomException>
    {
        protected override IHttpActionResult Handle(MyCustomException exception, HttpRequestMessage request)
        {
            HttpResponseMessage message = request.CreateResponse(HttpStatusCode.BadRequest, exception.GetType().FullName);
            return new ResponseMessageResult(message);
        }
    }

}