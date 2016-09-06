using DotJEM.Web.Host.Diagnostics.Performance.Trackers;

namespace DotJEM.Web.Host.Diagnostics.Performance.Ext
{
    public static class PerformanceLoggerBasicExtensions
    {
        public static IPerformanceTracker TrackTask(this IPerformanceLogger self, string name) => self.Track("task", name);
    }
}