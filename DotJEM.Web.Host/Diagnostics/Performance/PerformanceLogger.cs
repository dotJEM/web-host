using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Correlations;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly ILogWriter writer;

        public bool Enabled { get; }

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration)
        {
            if (configuration.Diagnostics?.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;
            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker Track(string type, params object[] args)
        {
            if (!Enabled)
                return NullPerformanceTracker.SharedInstance;

            return PerformanceTracker.Create(LogPerformanceEvent, CorrelationScope.Current.Branch(), type, args);
        }

        public void LogSingleEvent(string type, long elapsed, params object[] args)
        {
            PerformanceEvent.Execute(LogPerformanceEvent, type, CorrelationScope.Current.Branch(), elapsed, args);
        }

        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }

        private void ScopeCompleted()
        {
            
        }

        public ICorrelationScope StartCorrelationScope() => StartCorrelationScope(Guid.NewGuid());

        public ICorrelationScope StartCorrelationScope(Guid scopeid) => new CorrelationScope(scopeid, ScopeCompleted);
    }
}