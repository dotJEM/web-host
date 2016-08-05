using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Tasks
{
    /// <summary>
    /// NOTE: This is meant for async -> sync integration, it's recomended to elevtate async patterns all the way, but
    ///       this is not always possible during refactoring of old code bases. This is also why these are not added as convinient extension methods.
    /// </summary>
    public static class Sync
    {
        public static T Await<T>(Task<T> task)
        {
            try
            {
                return Task.Run(() => task).Result;

                //return self.Timeout(timeout).ConfigureAwait(false).GetAwaiter().GetResult();
                //return self.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                throw;
            }
        }

        public static T[] Await<T>(IEnumerable<Task<T>> tasks)
        {
            try
            {
                return Task.Run(() => Task.WhenAll(tasks)).Result;
                //return self.Timeout(timeout).ConfigureAwait(false).GetAwaiter().GetResult();
                //return self.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                throw;
            }
        }

        public static T[] Await<T>(params Task<T>[] tasks) => Await((IEnumerable<Task<T>>) tasks);


        //public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        //{
        //    CancellationTokenSource tokenSource = new CancellationTokenSource();
        //    Task completed = await Task.WhenAny(task, Task.Delay(timeout, tokenSource.Token));
        //    if (completed == task)
        //    {
        //        tokenSource.Cancel();
        //        return await task;  // Very important in order to propagate exceptions
        //    }
        //    throw new TimeoutException("The operation has timed out.");
        //}

        //public static async Task Timeout(this Task task, TimeSpan timeout)
        //{
        //    CancellationTokenSource tokenSource = new CancellationTokenSource();
        //    Task completed = await Task.WhenAny(task, Task.Delay(timeout, tokenSource.Token));
        //    if (completed == task)
        //    {
        //        tokenSource.Cancel();
        //        await task;  // Very important in order to propagate exceptions
        //    }
        //    throw new TimeoutException("The operation has timed out.");
        //}

        public static void Await(Task task)
        {
            try
            {
                Task.Run(() => task).Wait();

                //return self.Timeout(timeout).ConfigureAwait(false).GetAwaiter().GetResult();
                //return self.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                throw;
            }
        }

        public static void Await(IEnumerable<Task> tasks)
        {
            try
            {
                Task.Run(() => Task.WhenAll(tasks)).Wait();
                //return self.Timeout(timeout).ConfigureAwait(false).GetAwaiter().GetResult();
                //return self.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                throw;
            }
        }

        public static void Await(params Task[] tasks) => Await((IEnumerable<Task>)tasks);
    }
}
