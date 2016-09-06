using System.Net.Http;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;

namespace DotJEM.Web.Host.Diagnostics.Performance.Ext
{
    public static class PerformanceLoggerHttpExtensions
    {
        public static IPerformanceTracker TrackRequest(this IPerformanceLogger self, HttpRequestMessage request)
        {
            return self.Track("request", request.Method.Method, request.RequestUri.ToString(), request.Content.Headers.ContentLength);
        }
    }
}