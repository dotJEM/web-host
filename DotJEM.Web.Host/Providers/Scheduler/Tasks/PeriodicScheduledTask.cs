using System;
using DotJEM.Web.Host.Diagnostics.Performance;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks
{
    public class PeriodicScheduledTask : ScheduledTask
    {
        private readonly TimeSpan delay;

        public PeriodicScheduledTask(string name, Action<bool> callback, TimeSpan delay, IPerformanceLogger perf)
            : base(name, callback, perf)
        {
            this.delay = delay;
        }

        public override IScheduledTask Start()
        {
            return RegisterWait(delay);
        }

        protected override bool ExecuteCallback(bool timedout)
        {
            bool success = base.ExecuteCallback(timedout);
            //TODO: Count exceptions, increase callback time if reoccurences.                
            RegisterWait(delay);
            return success;
        }
    }
}