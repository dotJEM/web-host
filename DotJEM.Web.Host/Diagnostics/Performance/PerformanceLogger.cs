using System.Diagnostics;
using System.IO;
using System.Net.Http;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        PerformanceTracker Track(HttpRequestMessage request);
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

            string dir = Path.GetDirectoryName(config.Path);
            Debug.Assert(dir != null, "dir != null");

            Directory.CreateDirectory(dir);
            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public PerformanceTracker Track(HttpRequestMessage request)
        {
            return new PerformanceTracker(request, LogPerformanceEvent);
        }

        private void LogPerformanceEvent(PerformanceTracker tracker)
        {
            if (!Enabled)
                return;

            writer.Write(tracker.ToString());
        }
    }
}