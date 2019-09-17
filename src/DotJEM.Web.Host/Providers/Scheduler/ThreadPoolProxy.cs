using System;
using System.Threading;

namespace DotJEM.Web.Host.Providers.Scheduler
{
    /// <summary>
    /// Interface and implementation for enabling mocking.
    /// </summary>
    public interface IThreadPool
    {
        RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle handle, WaitOrTimerCallback callback, object state, TimeSpan timeout, bool executeOnlyOnce);
    }

    public class ThreadPoolProxy : IThreadPool
    {
        public RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle handle, WaitOrTimerCallback callback, object state, TimeSpan timeout, bool executeOnlyOnce)
        {
            return ThreadPool.RegisterWaitForSingleObject(handle, callback, state, timeout, executeOnlyOnce);
        }
    }
}