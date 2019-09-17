using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using DotJEM.Web.Host.Results;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace DotJEM.Web.Host
{
    public abstract class WebHostHubController<THub> : WebHostApiController where THub : IHub
    {
        private readonly Lazy<IHubContext> hub = new Lazy<IHubContext>(() => GlobalHost.ConnectionManager.GetHubContext<THub>());

        protected IHubContext Hub { get { return hub.Value; } }
    }

    public abstract class WebHostApiController : ApiController
    {
        protected virtual NotFoundErrorMessageResult NotFound(string message)
        {
            return new NotFoundErrorMessageResult(HttpStatusCode.NotFound, message, this);
        }

        protected virtual ForbiddenErrorMessageResult Forbidden(string message)
        {
            return new ForbiddenErrorMessageResult(HttpStatusCode.Forbidden, message, this);
        }


        protected virtual ServiceUnavailableMessageResult ServiceUnavailable(string message)
        {
            return new ServiceUnavailableMessageResult(HttpStatusCode.ServiceUnavailable, message, this);
        }

        protected dynamic FromFile(string path, string mediaType)
        {
            if (!File.Exists(path))
                return NotFound();

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(File.ReadAllBytes(path));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("file")
            {
                FileName = Path.GetFileName(path)
            };
            return response;
        }
    }
}