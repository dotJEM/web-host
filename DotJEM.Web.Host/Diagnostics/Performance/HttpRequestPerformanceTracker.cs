using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceTracker<in TFinalizer>
    {
        void Trace(TFinalizer final);
    }

    public abstract class PerformanceTracker<TFinalizer> : IPerformanceTracker<TFinalizer>
    {
        private readonly Action<string> completed;

        protected DateTime Time { get; private set; }
        protected string Identity { get; private set; }

        protected long ElapsedTicks { get { return end - start; } }

        private readonly long start;
        private long end = -1;

        protected PerformanceTracker(Action<string> completed)
        {
            start = Stopwatch.GetTimestamp();

            this.completed = completed;

            Time = DateTime.UtcNow;
            Identity = ClaimsPrincipal.Current.Identity.Name;
            if (string.IsNullOrEmpty(Identity))
                Identity = "N/A";
        }

        protected void Commit(Func<string> value)
        {
            end = Stopwatch.GetTimestamp();
            Task.Run(() => completed(value()));
        }

        public abstract void Trace(TFinalizer final);
    }

    public class TaskPerformanceTracker : PerformanceTracker<object>
    {
        private readonly string name;

        public TaskPerformanceTracker(Action<string> completed, string name) : base(completed)
        {
            this.name = name;
        }

        public override void Trace(object final)
        {
            Commit(ToString);
        }

        public override string ToString()
        {
            return string.Format("{0:s}\t{1}\t{2}\t{3}", Time, ElapsedTicks, Identity, name);
        }
    }

    public class HttpRequestPerformanceTracker : PerformanceTracker<HttpStatusCode>
    {
        private readonly string uri;
        private readonly string method;

        private HttpStatusCode status;

        public HttpRequestPerformanceTracker(Action<string> completed, HttpRequestMessage request) 
            : base(completed)
        {
            method = request.Method.Method;
            uri = request.RequestUri.ToString();
        }

        public override void Trace(HttpStatusCode final)
        {
            status = final;
            Commit(ToString);
        }

        public override string ToString()
        {
            return string.Format("{0:s}\t{1}\t{2}\t{3}\t{4}\t{5}", Time, ElapsedTicks, Identity, method, uri, status);
        }
    }
}