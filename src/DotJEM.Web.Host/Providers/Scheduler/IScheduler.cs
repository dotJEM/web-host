﻿using System;
using System.Collections.Concurrent;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;

namespace DotJEM.Web.Host.Providers.Scheduler
{
    public interface IWebScheduler
    {
        IScheduledTask Schedule(IScheduledTask task);
        IScheduledTask ScheduleTask(string name, Action<bool> callback, TimeSpan interval);
        IScheduledTask ScheduleCallback(string name, Action<bool> callback, TimeSpan? timeout = null);
        IScheduledTask ScheduleCron(string name, Action<bool> callback, string trigger);

        void Stop();
    }

    public class WebScheduler : IWebScheduler
    {
        private readonly ConcurrentDictionary<Guid, IScheduledTask> tasks = new ConcurrentDictionary<Guid, IScheduledTask>();

        private readonly ILogger perf;
        private readonly IDiagnosticsLogger logger;

        public WebScheduler(IDiagnosticsLogger logger, ILogger perf)
        {
            this.logger = logger;
            this.perf = perf;
        }

        public IScheduledTask Schedule(IScheduledTask task)
        {
            task.TaskException += HandleTaskException;
            task.TaskCompleted += HandleTaskCompleted;
            tasks.TryAdd(task.Id, task);
            return task.Start();
        }

        public IScheduledTask ScheduleTask(string name, Action<bool> callback, TimeSpan interval)
        {
            return Schedule(new PeriodicScheduledTask(name, callback, interval, perf));
        }

        public IScheduledTask ScheduleCallback(string name, Action<bool> callback, TimeSpan? timeout)
        {
            return Schedule(new SingleFireScheduledTask(name, callback, timeout, perf));
        }

        public IScheduledTask ScheduleCron(string name, Action<bool> callback, string trigger)
        {
            return Schedule(new CronScheduledTask(name, callback, trigger, perf));
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
}