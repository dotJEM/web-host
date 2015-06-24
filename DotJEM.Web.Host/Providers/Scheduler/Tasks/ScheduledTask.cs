using System;
using System.Diagnostics;
using System.Threading;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Providers.Concurrency;
using NCrontab;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks
{
    public interface IScheduledTask : IDisposable
    {
        event EventHandler<TaskEventArgs> TaskCompleted;
        event EventHandler<TaskExceptionEventArgs> TaskException;

        Guid Id { get; }
        string Name { get; }

        IScheduledTask Start();
        IScheduledTask Signal();
    }

    public abstract class ScheduledTask : Disposeable, IScheduledTask
    {
        public event EventHandler<TaskEventArgs> TaskCompleted;
        public event EventHandler<TaskExceptionEventArgs> TaskException;

        private readonly Action<bool> callback;
        private readonly AutoResetEvent handle = new AutoResetEvent(false);

        private Exception exception;
        private RegisteredWaitHandle executing;

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private readonly IThreadPool pool;
        private readonly IPerformanceLogger perf;

        protected ScheduledTask(string name, Action<bool> callback, IPerformanceLogger perf)
            : this(name, callback, new ThreadPoolProxy(), perf)
        {
        }

        protected ScheduledTask(string name, Action<bool> callback, IThreadPool pool, IPerformanceLogger perf)
        {
            Id = Guid.NewGuid();
            this.Name = name;
            this.callback = callback;
            this.pool = pool;
            this.perf = perf;
        }

        public abstract IScheduledTask Start();

        protected virtual IScheduledTask RegisterWait(TimeSpan timeout)
        {
            executing = pool.RegisterWaitForSingleObject(handle, (state, timedout) => ExecuteCallback(timedout), null, timeout, true);
            return this;
        }

        protected virtual bool ExecuteCallback(bool timedout)
        {
            if (Disposed) return false;
            try
            {
                IPerformanceTracker<object> tracker = perf.TrackTask(Name);
                callback(!timedout);
                tracker.Trace(null);
                return true;
            }
            catch (Exception ex)
            {
                bool seenBefore = exception != null && exception.GetType() == ex.GetType();
                exception = ex;

                OnTaskException(new TaskExceptionEventArgs(ex, this, seenBefore));
                return false;
            }
        }

        protected virtual void OnTaskException(TaskExceptionEventArgs args)
        {
            EventHandler<TaskExceptionEventArgs> handler = TaskException;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected virtual void OnTaskCompleted(TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = TaskCompleted;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            OnTaskCompleted(new TaskEventArgs(this));
        }

        public virtual IScheduledTask Signal()
        {
            handle.Set();
            return this;
        }
    }

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

    public class CronScheduledTask : ScheduledTask
    {
        private readonly CrontabSchedule trigger;

        public CronScheduledTask(string name, Action<bool> callback, string trigger, IPerformanceLogger perf)
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

    public class SingleFireScheduledTask : ScheduledTask
    {
        private readonly TimeSpan delay;

        public override IScheduledTask Start()
        {
            return RegisterWait(delay);
        }

        public SingleFireScheduledTask(string name, Action<bool> callback, TimeSpan? delay, IPerformanceLogger perf)
            : base(name, callback, perf)
        {
            this.delay = delay ?? TimeSpan.Zero;
        }
    }
}