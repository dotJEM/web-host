using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Diagnostics.ExceptionHandlers
{
    public interface IWebHostExceptionHandler
    {
        Type ExceptionType { get; }
        IHttpActionResult Handle(ExceptionHandlerContext context);
    }

    public abstract class GenericExceptionHandler<T> : IWebHostExceptionHandler where T : Exception
    {
        public Type ExceptionType { get { return typeof(T); } }

        public virtual IHttpActionResult Handle(ExceptionHandlerContext context)
        {
            

            return Handle((T) context.Exception, context.Request);
        }

        protected abstract IHttpActionResult Handle(T exception, HttpRequestMessage request);
    }
}