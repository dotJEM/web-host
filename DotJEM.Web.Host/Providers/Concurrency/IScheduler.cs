using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using DotJEM.Web.Host.Diagnostics;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IWebScheduler
    {
        IScheduledTask Schedule(IScheduledTask task);
        IScheduledTask Schedule(Action<bool> task, TimeSpan timeout);
    }

    public class WebScheduler : IWebScheduler
    {
        private readonly IDictionary<Guid, IScheduledTask> tasks = new ConcurrentDictionary<Guid, IScheduledTask>();

        private IDiagnosticsLogger logger;

        public WebScheduler(IDiagnosticsLogger logger)
        {
            this.logger = logger;
        }

        public IScheduledTask Schedule(IScheduledTask task)
        {
            tasks.Add(task.Id, task);
            return task.Start();
        }

        public IScheduledTask Schedule(Action<bool> task, TimeSpan timeout)
        {
            return Schedule(new PeriodicScheduledTask(Guid.NewGuid(), task, timeout));
        }
    }


    public interface IScheduledTask : IDisposable
    {
        event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        Guid Id { get; }

        IScheduledTask Start();
        IScheduledTask Execute();
        IScheduledTask Signal();
    }

    public abstract class ScheduledTask : Disposeable, IScheduledTask
    {
        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        private readonly TimeSpan delay;
        private readonly Action<bool> callback;
        private readonly AutoResetEvent handle = new AutoResetEvent(false);
       
        private RegisteredWaitHandle executing;
        private Exception exception;

        public Guid Id { get; private set; }

        protected ScheduledTask(Guid id, Action<bool> callback, TimeSpan? delay)
        {
            Id = id;
            this.callback = callback;
            this.delay = delay ?? TimeSpan.Zero;
        }

        public virtual IScheduledTask Start()
        {
            executing = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedout) => ExecuteCallback(timedout), null, delay, true);
            return this;
        }

        protected virtual bool ExecuteCallback(bool timedout)
        {
            if (Disposed)
                return false;

            try
            {
                callback(!timedout);
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                OnUnhandledException(new UnhandledExceptionEventArgs(ex, true));
                return false;
            }
        }

        public virtual IScheduledTask Execute()
        {
            ExecuteCallback(false);
            return this;
        }

        protected virtual void OnUnhandledException(UnhandledExceptionEventArgs args)
        {
            var handler = UnhandledException;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public virtual IScheduledTask Signal()
        {
            handle.Set();
            return this;
        }
    }

    public class PeriodicScheduledTask : ScheduledTask
    {
        public PeriodicScheduledTask(Guid id, Action<bool> callback, TimeSpan delay) 
            : base(id, callback, delay)
        {
        }

        protected override bool ExecuteCallback(bool timedout)
        {
            bool success = base.ExecuteCallback(timedout);
            //TODO: Count exceptions, increase callback time if reoccurences.                
            Start();
            return success;
        }
    }

    public class CronScheduledTask : ScheduledTask
    {
        public CronScheduledTask(Guid id, Action<bool> callback, TimeSpan delay)
            : base(id, callback, delay)
        {
        }

        protected override bool ExecuteCallback(bool timedout)
        {
            bool success = base.ExecuteCallback(timedout);
            //TODO: Count exceptions, increase callback time if reoccurences.                
            Start();
            return success;
        }
    }
}