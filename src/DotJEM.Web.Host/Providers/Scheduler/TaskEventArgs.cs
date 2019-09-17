using System;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;

namespace DotJEM.Web.Host.Providers.Scheduler
{
    public class TaskEventArgs : EventArgs
    {
        public IScheduledTask Task { get; private set; }

        public TaskEventArgs(IScheduledTask task)
        {
            Task = task;
        }
    }

    public class TaskExceptionEventArgs : TaskEventArgs
    {
        public Exception Exception { get; private set; }

        public TaskExceptionEventArgs(Exception exception, IScheduledTask task, bool seenBefore)
            : base(task)
        {
            Exception = exception;
        }
    }
}