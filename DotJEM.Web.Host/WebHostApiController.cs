using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using DotJEM.Web.Host.ActionResults;

namespace DotJEM.Web.Host
{
    public abstract class WebHostApiController : ApiController
    {
        protected virtual NotFoundErrorMessageResult NotFound(string message)
        {
            return new NotFoundErrorMessageResult(HttpStatusCode.NotFound, message, this);
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