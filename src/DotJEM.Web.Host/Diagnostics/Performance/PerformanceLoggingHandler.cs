using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Diagnostic.Correlation;
using IPerformanceTracker = DotJEM.Web.Host.Diagnostics.Performance.Trackers.IPerformanceTracker;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLoggingHandler : DelegatingHandler
    {
        private readonly IPerformanceLogger logger;
        private ILogger newLogger;

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
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(true);

            using (new CorrelationScope(request.GetCorrelationId()))
            {
                using (Diagnostic.IPerformanceTracker tracker = newLogger.TrackRequest(request))
                {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(true);
                    tracker.Commit(new { statusCode = response.StatusCode });
                    return response;
                }
            }

            //IPerformanceTracker tracker = logger.TrackRequest(request);
            //HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(true);
            //tracker.Commit(response.StatusCode);
            //return response;
        }
    }

    public static class HttpRequestLoggerExt {
        public static Diagnostic.IPerformanceTracker TrackRequest(this ILogger self, HttpRequestMessage request)
        {
            return self.Track("request", new
            {
                method = request.Method.Method,
                uri = request.RequestUri.ToString(),
                contentType = request.Content.Headers.ContentType,
                contentLength = request.Content.Headers.ContentLength
            });
        }
    }

}
