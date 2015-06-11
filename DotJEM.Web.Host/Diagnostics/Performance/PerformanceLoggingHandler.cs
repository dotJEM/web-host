using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLoggingHandler : DelegatingHandler
    {
        private readonly IPerformanceLogger logger;

        public PerformanceLoggingHandler(IPerformanceLogger logger)
        {
            this.logger = logger;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(!logger.Enabled)
                return await base.SendAsync(request, cancellationToken);
            
            PerformanceTracker tracker = logger.Track(request);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            tracker.Trace(response.StatusCode);
            return response;
        }
    }
}
