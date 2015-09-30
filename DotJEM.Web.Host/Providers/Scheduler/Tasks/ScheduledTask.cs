using System;
using System.Diagnostics;
using System.Threading;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Providers.Concurrency;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Scheduler.Tasks
{
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
            //TODO: (jmd 2015-09-30) Consider wrapping in try catch. They can force the thread to close the app. 
            EventHandler<TaskExceptionEventArgs> handler = TaskException;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected virtual void OnTaskCompleted(TaskEventArgs args)
        {
            //TODO: (jmd 2015-09-30) Consider wrapping in try catch. They can force the thread to close the app. 
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

        public IScheduledTask Signal(TimeSpan delay)
        {
            pool.RegisterWaitForSingleObject(new AutoResetEvent(false), (state, tout) => Signal(), null, delay, true);
            return this;
        }
    }
}