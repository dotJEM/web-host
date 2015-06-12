using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        IPerformanceTracker<HttpStatusCode> TrackRequest(HttpRequestMessage request);
        IPerformanceTracker<object> TrackTask(string name);
    }

    public class PerformanceLogger : IPerformanceLogger
    {
        public bool Enabled { get; private set; }

        private readonly ILogWriter writer;

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration)
        {
            //TODO: Null logger pattern
            if (configuration.Diagnostics == null || configuration.Diagnostics.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;

            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker<HttpStatusCode> TrackRequest(HttpRequestMessage request)
        {
            return new HttpRequestPerformanceTracker(LogPerformanceEvent, request);
        }

        public IPerformanceTracker<object> TrackTask(string name)
        {
            return new TaskPerformanceTracker(LogPerformanceEvent, name);
        }

        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }
    }
}