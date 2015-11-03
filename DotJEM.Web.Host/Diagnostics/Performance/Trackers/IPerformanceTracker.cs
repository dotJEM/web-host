using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance.Trackers
{
    public interface IPerformanceTracker
    {
        void Commit(params object[] args);
        void Commit(Func<object[]> argsFactory);
    }

    public class PerformanceTracker : IPerformanceTracker
    {
        private readonly Action<string> completed;

        protected DateTime Time { get; }
        protected string Identity { get; }

        protected long ElapsedTicks => end - start;
        protected long ElapsedMilliseconds => (ElapsedTicks * 1000) / Stopwatch.Frequency;

        private readonly string type;
        private readonly string[] arguments;

        private readonly long start;
        private long end = -1;

        public PerformanceTracker(Action<string> completed, string type, params object[] arguments)
        {
            start = Stopwatch.GetTimestamp();

            this.completed = completed;
            this.type = type;
            this.arguments = Array.ConvertAll(arguments, obj => (obj ?? "N/A").ToString());

            Time = DateTime.UtcNow;
            Identity = ClaimsPrincipal.Current.Identity.Name;
        }

        public void Commit(Func<object[]> argsFactory)
        {
            end = Stopwatch.GetTimestamp();
            Task.Run(() => Complete(argsFactory())).ConfigureAwait(false);
        }

        public void Commit(params object[] args)
        {
            end = Stopwatch.GetTimestamp();
            Task.Run(() => Complete(args)).ConfigureAwait(false);
        }

        private void Complete(params object[] args)
        {
            completed(Format(args));
        }

        private string Format(object[] args)
        {
            args = arguments.Union(args).ToArray();
            string identity = string.IsNullOrEmpty(Identity) ? "NO IDENTITY" : Identity;
            string[] prefix = { Time.ToString("s"), ElapsedMilliseconds.ToString(), type, identity };
            return string.Join("\t", prefix.Union(args));
        }
    }
}