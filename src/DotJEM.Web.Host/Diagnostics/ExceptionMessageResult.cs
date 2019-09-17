using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace DotJEM.Web.Host.Diagnostics
{
    public class ExceptionMessageResult : IHttpActionResult
    {
        private readonly HttpResponseMessage message;

        public ExceptionMessageResult(HttpResponseMessage message)
        {
            this.message = message;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(message);
        }
    }
}