using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceTracker 
    {
        private readonly Action<PerformanceTracker> completed;

        private readonly string method;
        private readonly string uri;
        private readonly string user;
        private readonly long start;
        private readonly DateTime time;

        private HttpStatusCode status;
        private long end;

        public PerformanceTracker(HttpRequestMessage request, Action<PerformanceTracker> completed)
        {
            this.completed = completed;
            time = DateTime.UtcNow;
            method = request.Method.Method;
            uri = request.RequestUri.ToString();
            user = ClaimsPrincipal.Current.Identity.Name;
            start = Stopwatch.GetTimestamp();
        }

        public void Trace(HttpStatusCode status)
        {
            this.status = status;
            end = Stopwatch.GetTimestamp();

            Task.Run(() => completed(this));
        }

        public override string ToString()
        {
            return string.Format("{0:s}, {1}, {2}, {3}, {4}, {5}", time, method, uri, status, user, end - start);
        }
    }
}