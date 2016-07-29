using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance.Trackers
{
    public interface IPerformanceTracker : IDisposable
    {
        void Commit(params object[] args);
        void Commit(Func<object[]> argsFactory);
        void PushArgs(params object[] args);
    }

    public class NullPerformanceTracker : IPerformanceTracker
    {
        public static IPerformanceTracker SharedInstance { get; } = new NullPerformanceTracker();

        public void Commit(params object[] args) {}
        public void Commit(Func<object[]> argsFactory) {}
        public void PushArgs(params object[] args) {}

        public void Dispose() { }
    }

    public class PerformanceTracker : IPerformanceTracker
    {
        private readonly Action<string> completed;
        private readonly string correlationId;

        protected DateTime Time { get; }
        protected string Identity { get; }

        protected long ElapsedTicks => end - start;
        protected long ElapsedMilliseconds => (ElapsedTicks * 1000) / Stopwatch.Frequency;

        private readonly string type;
        private string[] arguments;

        private readonly long start;
        private long end = -1;
        private volatile bool comitted = false;

        public static IPerformanceTracker Create(Action<string> completed, string correlationId, string type, params object[] arguments)
            => new PerformanceTracker(completed, correlationId, type, arguments);

        public PerformanceTracker(Action<string> completed, string correlationId, string type, params object[] arguments)
        {
            start = Stopwatch.GetTimestamp();

            this.completed = completed;
            this.correlationId = correlationId;
            this.type = type;
            this.arguments = Array.ConvertAll(arguments, obj => (obj ?? "N/A").ToString());

            Time = DateTime.UtcNow;
            Identity = ClaimsPrincipal.Current.Identity.Name;
        }

        public void Commit(Func<object[]> argsFactory)
        {
            if (comitted)
                return;

            comitted = true;
            end = Stopwatch.GetTimestamp();
            Task.Run(() => Complete(argsFactory())).ConfigureAwait(false);
        }

        public void PushArgs(params object[] args)
        {
            this.arguments = arguments.Union(Array.ConvertAll(arguments, obj => (obj ?? "N/A").ToString())).ToArray();
        }

        public void Commit(params object[] args) => Commit(()=>args);
        private void Complete(params object[] args) => completed(Format(args));

        private string Format(object[] args)
        {
            args = arguments.Union(args).ToArray();
            string identity = string.IsNullOrEmpty(Identity) ? "NO IDENTITY" : Identity;
            string[] prefix = { Time.ToString("s"), ElapsedMilliseconds.ToString(), type, correlationId, identity };
            return string.Join("\t", prefix.Union(args));
        }

        public void Dispose() => Commit();
    }

    public interface IPerformanceEvent : IDisposable
    {
    }

    public class PerformanceEvent : IPerformanceEvent
    {
        private readonly string type;
        private readonly string[] arguments;
        private readonly Action<string> completed;
        private readonly string correlationId;

        private long ElapsedMilliseconds { get; }
        private DateTime Time { get; }
        private string Identity { get; }
        private volatile bool comitted = false;

        public static IPerformanceEvent Create(Action<string> completed, string type, string correlationId, long elapsed, params object[] arguments)
            => new PerformanceEvent(completed, correlationId, type, elapsed, arguments);

        public static void Execute(Action<string> completed, string type, string correlationId, long elapsed, params object[] arguments)
            => new PerformanceEvent(completed, correlationId, type, elapsed, arguments).Dispose();

        public PerformanceEvent(Action<string> completed, string correlationId, string type, long elapsed, params object[] arguments)
        {
            this.completed = completed;
            this.correlationId = correlationId;
            this.type = type;
            this.arguments = Array.ConvertAll(arguments, obj => (obj ?? "N/A").ToString());

            ElapsedMilliseconds = elapsed;
            Time = DateTime.UtcNow;
            Identity = ClaimsPrincipal.Current.Identity.Name;
        }

        private string Format()
        {
            string identity = string.IsNullOrEmpty(Identity) ? "NO IDENTITY" : Identity;
            string[] prefix = { Time.ToString("s"), ElapsedMilliseconds.ToString(), type, correlationId, identity };
            return string.Join("\t", prefix.Union(arguments));
        }

        private void Complete() => completed(Format());

        private void Commit()
        {
            if (comitted)
                return;

            comitted = true;
            Task.Run(() => Complete()).ConfigureAwait(false);
        }

        public void Dispose() => Commit();

    }
}