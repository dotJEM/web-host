using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Tasks;

/// <summary>
/// NOTE: This is meant for async -> sync integration, it's recomended to elevtate async patterns all the way, but
///       this is not always possible during refactoring of old code bases. This is also why these are not added as convinient extension methods.
/// </summary>
public static class Sync
{
    public static async void FireAndForget(Task task, Action<Exception> onException = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            onException?.Invoke(e);
        }
    }

    public static async void FireAndForget<T>(Task<T> task, Action<Exception> onException = null, Action<T> onValue = null)
    {
        try
        {
            T value = await task.ConfigureAwait(false);
            onValue?.Invoke(value);
        }
        catch (Exception e)
        {
            onException?.Invoke(e);
        }
    }

    public static T Await<T>(Task<T> task)
    {
        using (new NoSynchronizationContext())
        {
            try
            {
                return task.Result;
                //return Task.Run(() => task).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                // ReSharper disable HeuristicUnreachableCode
                // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                throw;
                // ReSharper restore HeuristicUnreachableCode
            }
        }
    }

    public static T[] Await<T>(IEnumerable<Task<T>> tasks)
    {
        using (new NoSynchronizationContext())
        {
            try
            {
                return Task.WhenAll(tasks).Result;
                //return Task.Run(() => Task.WhenAll(tasks)).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                // ReSharper disable HeuristicUnreachableCode
                // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                throw;
                // ReSharper restore HeuristicUnreachableCode
            }
        }
    }

    public static T[] Await<T>(params Task<T>[] tasks) => Await((IEnumerable<Task<T>>) tasks);

    public static void Await(Task task)
    {
        using (new NoSynchronizationContext())
        {
            try
            {
                task.Wait();
                //Task.Run(() => task).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                // ReSharper disable HeuristicUnreachableCode
                // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                throw;
                // ReSharper restore HeuristicUnreachableCode
            }
        }
    }

    public static void Await(IEnumerable<Task> tasks)
    {
        using (new NoSynchronizationContext())
        {
            try
            {
                Task[] tasks2 = tasks.ToArray();

                Task.WhenAll(tasks2).Wait();
                Debug.WriteLine("");
                //Task.Run(() => Task.WhenAll(tasks)).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                // ReSharper disable HeuristicUnreachableCode
                // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                throw;
                // ReSharper restore HeuristicUnreachableCode
            }
        }
    }

    public static void Await(params Task[] tasks) => Await((IEnumerable<Task>)tasks);

    private class NoSynchronizationContext : IDisposable
    {
        private readonly SynchronizationContext context;

        public NoSynchronizationContext()
        {
            context = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }
        public void Dispose() =>
            SynchronizationContext.SetSynchronizationContext(context);
    }
}