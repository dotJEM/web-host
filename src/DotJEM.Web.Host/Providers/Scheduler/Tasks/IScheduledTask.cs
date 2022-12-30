using NCrontab;
using System;
using System.Net.Configuration;
using DotJEM.AdvParsers;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks;



public interface ITrigger
{
    bool TryGetNext(bool firstExecution, out TimeSpan timeSpan);
}

public abstract class Trigger : ITrigger
{
    public static ITrigger Parse(string value)
    {
        if (TryParse(value, out ITrigger trigger))
            return trigger;
        throw new FormatException("Invalid trigger format.");
    }

    public static bool TryParse(string value, out ITrigger trigger)
    {
        string[] parts = value.Split(':');
        if (parts.Length > 1)
        {
            if (parts[0].Equals("cron", StringComparison.OrdinalIgnoreCase))
            {
                trigger = new CronTrigger(CrontabSchedule.Parse(parts[1]));
                return true;
            }

            if (parts[0].Equals("span", StringComparison.OrdinalIgnoreCase))
            {
                trigger= new PeriodicTrigger(AdvParser.ParseTimeSpan(parts[1]));
                return true;
            }
        }

        CrontabSchedule schedule = null;
        if (CrontabSchedule.TryParse(value, v =>
            {
                schedule = v;
                return true;
            }, e => false))
        {
            trigger= new CronTrigger(schedule);
            return true;
        }

        if (AdvParser.TryParseTimeSpan(value, out TimeSpan span))
        {
            trigger = new PeriodicTrigger(span);
            return true;
        }

        trigger = null;
        return false;
    }

    public abstract bool TryGetNext(bool firstExecution, out TimeSpan timeSpan);
}

public class CronTrigger : ITrigger
{
    private readonly CrontabSchedule cron;

    public CronTrigger(CrontabSchedule cron)
    {
        this.cron = cron;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = cron.GetNextOccurrence(DateTime.Now).Subtract(DateTime.Now);
        return true;
    }
}

public class PeriodicTrigger : ITrigger
{
    private readonly TimeSpan timeSpan;

    public PeriodicTrigger(TimeSpan timeSpan)
    {
        this.timeSpan = timeSpan;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = this.timeSpan;
        return true;
    }
}

public class SingleFireTrigger : ITrigger
{
    private readonly TimeSpan timeSpan;

    public SingleFireTrigger(TimeSpan timeSpan)
    {
        this.timeSpan = timeSpan;
    }
    
    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = this.timeSpan;
        return firstExecution;
    }
}

