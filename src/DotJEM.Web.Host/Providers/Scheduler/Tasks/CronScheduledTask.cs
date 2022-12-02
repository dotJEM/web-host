using System;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;
using NCrontab;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks;

public class CronScheduledTask : ScheduledTask
{
    private readonly CrontabSchedule trigger;

    public CronScheduledTask(string name, Action<bool> callback, string trigger, ILogger perf)
        : base(name, callback, perf)
    {
        this.trigger = CrontabSchedule.Parse(trigger);
    }

    public override IScheduledTask Start()
    {
        return RegisterWait(Next());
    }

    private TimeSpan Next()
    {
        return trigger.GetNextOccurrence(DateTime.Now).Subtract(DateTime.Now);
    }

    protected override bool ExecuteCallback(bool timedout)
    {
        bool success = base.ExecuteCallback(timedout);
        //TODO: Count exceptions, increase callback time if reoccurences.                
        RegisterWait(Next());
        return success;
    }


}