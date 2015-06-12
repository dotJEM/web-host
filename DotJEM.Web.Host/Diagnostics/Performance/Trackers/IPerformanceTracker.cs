using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance.Trackers
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

        protected abstract string Type { get; }

        private readonly long start;
        private long end = -1;

        protected PerformanceTracker(Action<string> completed)
        {
            start = Stopwatch.GetTimestamp();

            this.completed = completed;

            Time = DateTime.UtcNow;
            Identity = ClaimsPrincipal.Current.Identity.Name;
        }

        protected void Commit(Func<string> value)
        {
            end = Stopwatch.GetTimestamp();
            Task.Run(() => completed(value()));
        }

        protected void Commit(params string[] args)
        {
            end = Stopwatch.GetTimestamp();
            Task.Run(() => completed(Format(args)));
        }

        private string Format(string[] args)
        {
            string iden = string.IsNullOrEmpty(Identity) ? "NO IDENTITY" : Identity;
            string[] prefix = { Time.ToString("s"), ElapsedTicks.ToString(), Type, iden };
            return string.Join("\t", prefix.Union(args));
        }

        public abstract void Trace(TFinalizer final);
    }

    public class TaskPerformanceTracker : PerformanceTracker<object>
    {
        private readonly string name;

        protected override string Type{get { return "task"; }}

        public TaskPerformanceTracker(Action<string> completed, string name)
            : base(completed)
        {
            this.name = name;
        }

        public override void Trace(object final)
        {
            Commit(name);
        }
    }

    public class HttpRequestPerformanceTracker : PerformanceTracker<HttpStatusCode>
    {
        private readonly string uri;
        private readonly string method;

        protected override string Type { get { return "request"; } }

        public HttpRequestPerformanceTracker(Action<string> completed, HttpRequestMessage request)
            : base(completed)
        {
            method = request.Method.Method;
            uri = request.RequestUri.ToString();
        }

        public override void Trace(HttpStatusCode final)
        {
            Commit(method, uri, final.ToString());
        }
    }
}