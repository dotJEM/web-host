using System;
using System.Collections.Concurrent;
using System.Web.Hosting;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Abstractions;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using NCrontab;

namespace DotJEM.Web.Host.Providers.Scheduler;

public interface IWebScheduler
{
    IScheduledTask Schedule(IScheduledTask task);

    IScheduledTask Schedule(string name, Action<bool> callback, string expression);

    IScheduledTask ScheduleTask(string name, Action<bool> callback, TimeSpan interval);
    IScheduledTask ScheduleCallback(string name, Action<bool> callback, TimeSpan? timeout = null);
    IScheduledTask ScheduleCron(string name, Action<bool> callback, string trigger);

    void Stop();
}

public class WebScheduler : IWebScheduler, IRegisteredObject
{
    private readonly ConcurrentDictionary<Guid, IScheduledTask> tasks = new();

    private readonly ILogger perf;
    private readonly IDiagnosticsLogger logger;

    public WebScheduler(IDiagnosticsLogger logger, ILogger perf, IHostingEnvironment host)
    {
        this.logger = logger;
        this.perf = perf;
        host.RegisterObject(this);
    }


    public IScheduledTask Schedule(IScheduledTask task)
    {
        task.TaskException += HandleTaskException;
        task.TaskCompleted += HandleTaskCompleted;
        tasks.TryAdd(task.Id, task);
        return task.Start();
    }

    public IScheduledTask Schedule(string name, Action<bool> callback, string expression)
    {
        return Schedule(new ScheduledTask(name, callback, Trigger.Parse(expression), perf));
    }

    public IScheduledTask ScheduleTask(string name, Action<bool> callback, TimeSpan interval)
    {
        return Schedule(new ScheduledTask(name, callback, new PeriodicTrigger(interval), perf));
    }

    public IScheduledTask ScheduleCallback(string name, Action<bool> callback, TimeSpan? timeout)
    {
        return Schedule(new ScheduledTask(name, callback, new SingleFireTrigger(timeout ?? TimeSpan.Zero), perf));
    }

    public IScheduledTask ScheduleCron(string name, Action<bool> callback, string trigger)
    {
        return Schedule(new ScheduledTask(name, callback, new CronTrigger(CrontabSchedule.Parse(trigger)), perf));
    }

    private void HandleTaskCompleted(object sender, TaskEventArgs args)
    {
        IScheduledTask task;
        tasks.TryRemove(args.Task.Id, out task);
    }

    public void Stop()
    {
        foreach (IScheduledTask task in tasks.Values)
            task.Dispose();
    }

    void IRegisteredObject.Stop(bool immediate)
    {
        Stop();
    }

    private void HandleTaskException(object sender, TaskExceptionEventArgs args)
    {
        try
        {
            logger.LogException(args.Exception, new
            {
                TaskName = args.Task.Name,
                TaskId = args.Task.Id
            });
        }
        catch (Exception)
        {
            //ignore
            //TODO: (jmd 2015-09-30) Ohh shit, log to the event log or something!. 
        }
    }
}