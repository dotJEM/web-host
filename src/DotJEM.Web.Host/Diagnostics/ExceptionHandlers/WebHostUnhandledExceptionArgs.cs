using System;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace DotJEM.Web.Host.Diagnostics.ExceptionHandlers;

public class WebHostUnhandledExceptionArgs : EventArgs
{
    private readonly ExceptionHandlerContext context;
        
    public IHttpActionResult Result { get; set; }

    public ExceptionHandlerContext Context
    {
        get { return context; }
    }

    public WebHostUnhandledExceptionArgs(ExceptionHandlerContext context)
    {
        this.context = context;
    }
}