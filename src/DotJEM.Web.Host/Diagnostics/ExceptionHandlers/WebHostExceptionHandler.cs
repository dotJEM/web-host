using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using DotJEM.Web.Host.Providers.Pipeline;

namespace DotJEM.Web.Host.Diagnostics.ExceptionHandlers;

public class WebHostExceptionHandler : ExceptionHandler
{
    private readonly Dictionary<Type, IWebHostExceptionHandler> map;

    public event EventHandler<WebHostUnhandledExceptionArgs> UnhandledException;

    public WebHostExceptionHandler(IWebHostExceptionHandler[] handlers)
    {
        map = handlers.ToDictionary(handler => handler.ExceptionType);
    }

    private IWebHostExceptionHandler LookupHandler(Type exceptionType)
    {
        IWebHostExceptionHandler handler;
        if (map.TryGetValue(exceptionType, out handler))
            return handler;

        return exceptionType != typeof(Exception)
            ? LookupHandler(exceptionType.BaseType) 
            : null;
    }

    private IHttpActionResult ByHandlers(ExceptionHandlerContext context)
    {
        IWebHostExceptionHandler handler = LookupHandler(context.Exception.GetType());
        return handler?.Handle(context);
    }

    private IHttpActionResult ByDefault(ExceptionHandlerContext context)
    {
        HttpResponseMessage message = context.Request.CreateErrorResponse(HttpStatusCode.InternalServerError,context.Exception.Message, context.Exception);
        return new ExceptionMessageResult(message);
    }

    private IHttpActionResult ByEvent(ExceptionHandlerContext context)
    {
        WebHostUnhandledExceptionArgs args = OnUnhandledException(new WebHostUnhandledExceptionArgs(context));
        return args.Result;
    }

    public override Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(() =>
        {
            context.Result = ByHandlers(context) ?? ByEvent(context) ?? ByDefault(context);
        }, cancellationToken);
    }

    private WebHostUnhandledExceptionArgs OnUnhandledException(WebHostUnhandledExceptionArgs args)
    {
        UnhandledException?.Invoke(this, args);
        return args;
    }

    public override bool ShouldHandle(ExceptionHandlerContext context)
    {
        return true;
    }
}