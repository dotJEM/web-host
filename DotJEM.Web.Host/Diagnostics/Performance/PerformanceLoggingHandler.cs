using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLoggingHandler : DelegatingHandler
    {
        private readonly IPerformanceLogger logger;

        public PerformanceLoggingHandler(IPerformanceLogger logger)
        {
            this.logger = logger;
        }

        public PerformanceLoggingHandler(IPerformanceLogger logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!logger.Enabled)
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            IPerformanceTracker tracker = logger.TrackRequest(request);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            tracker.Commit(response.StatusCode);
            return response;
        }
    }

}
