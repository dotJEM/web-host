using System;
using DotJEM.Web.Host.Diagnostics.Performance.Correlations;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        IPerformanceTracker Track(string type, params object[] args);

        void LogSingleEvent(string type, long elapsed, params object[] args);

        ICorrelationScope StartCorrelationScope();
        ICorrelationScope StartCorrelationScope(Guid scopeid);
    }
}