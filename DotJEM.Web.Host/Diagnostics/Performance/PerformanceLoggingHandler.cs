using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.Correlation;
using DotJEM.Diagnostic.Writers;
using DotJEM.Json.Index.Util;
using DotJEM.Web.Host.Configuration.Elements;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface ILoggerFactory
    {
        ILogger Create();
    }

    public class LoggerFactory : ILoggerFactory
    {
        private IPathResolver resolver;
        private IWebHostConfiguration configuration;

        public LoggerFactory(IWebHostConfiguration configuration, IPathResolver resolver)
        {
            this.configuration = configuration;
            this.resolver = resolver;
        }

        public ILogger Create()
        {
            if (configuration.Diagnostics?.Performance == null)
                return new NullLogger();

            PerformanceConfiguration config = configuration.Diagnostics.Performance;
            var writer = new QueuingTraceWriter(resolver.MapPath(config.Path), AdvConvert.ConvertToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
            return new HighPrecisionLogger(new TraceEventCollector(writer));
        }
    }

    public class NullLogger : ILogger
    {
        public Task LogAsync(string type, object customData) => Task.CompletedTask;
    }

    public class PerformanceLoggingHandler : DelegatingHandler
    {
        private readonly ILogger logger;
        public PerformanceLoggingHandler(ILogger logger)
        {
            this.logger = logger;
        }

        public PerformanceLoggingHandler(ILogger logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!logger.IsEnabled())
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            using (new CorrelationScope(request.GetCorrelationId()))
            {
                using (IPerformanceTracker tracker = logger.TrackRequest(request))
                {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(true);
                    tracker.Commit(new { statusCode = response.StatusCode });
                    return response;
                }
            }
        }
    }

    public static class LoggerExtensions
    {
        public static bool IsEnabled(this ILogger self) => !(self is NullLogger);

        public static IPerformanceTracker TrackRequest(this ILogger self, HttpRequestMessage request)
        {
            return self.Track("request", new
            {
                method = request.Method.Method,
                uri = request.RequestUri.ToString(),
                contentType = request.Content.Headers.ContentType,
                contentLength = request.Content.Headers.ContentLength
            });
        }
        public static IPerformanceTracker TrackTask(this ILogger self, string name)
        {
            return self.Track("task", new { name });
        }
    }
}
