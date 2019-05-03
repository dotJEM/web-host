using System;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks
{
    public class SingleFireScheduledTask : ScheduledTask
    {
        private readonly TimeSpan delay;

        public override IScheduledTask Start()
        {
            return RegisterWait(delay);
        }

        public SingleFireScheduledTask(string name, Action<bool> callback, TimeSpan? delay, ILogger perf)
            : base(name, callback, perf)
        {
            this.delay = delay ?? TimeSpan.Zero;
        }
    }
}