using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Hosting;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IStorageIndexManager>().ImplementedBy<StorageIndexManager>().LifestyleSingleton());
        }
    }

    public interface IStorageIndexManager
    {
        event EventHandler<IndexChangesEventArgs> IndexChanged;

        void Start();
        void Stop();
        void QueueUpdate(JObject entity);
        void QueueDelete(JObject entity);
    }

    public class IndexChangesEventArgs : EventArgs
    {
        public IDictionary<string, IStorageChanges> Changes { get; private set; }

        public IndexChangesEventArgs(IDictionary<string, IStorageChanges> changes)
        {
            Changes = changes;
        }
    }

    public class StorageIndexManager : IStorageIndexManager
    {
        public event EventHandler<IndexChangesEventArgs> IndexChanged;

        private readonly IStorageIndex index;
        private readonly IDiagnosticsLogger logger;
        private readonly object padlock = new object();

        private Scheduler callback;
        private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly TimeSpan interval;
        private readonly string cachePath;
        private bool initialized;

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration, IDiagnosticsLogger logger)
        {
            this.index = index;
            this.logger = logger;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);
            foreach (WatchElement watch in configuration.Index.Watch.Items)
                logs[watch.Area] = storage.Area(watch.Area).Log;

            //TODO: Use the below to store a index pointer.
            if (!string.IsNullOrEmpty(configuration.Index.CacheLocation))
                cachePath = Path.Combine(HostingEnvironment.MapPath(configuration.Index.CacheLocation), "tracker");
        }

        public void Start()
        {
            UpdateIndex();
            callback = new Scheduler(signaled => UpdateIndex(), interval);
        }

        public void Stop()
        {
            callback.Dispose();
        }

        private void UpdateIndex()
        {
            try
            {
                IEnumerable<Tuple<string, IStorageChanges>> tuples;
                if (!initialized)
                {
                    Dictionary<string, long> tracker = InitializeFromTracker();
                    tuples = logs
                        .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get(GetTracker(tracker, log.Key))))
                        .ToList();
                    initialized = true;
                }
                else
                {
                    tuples = logs
                        .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get()))
                        .ToList();
                }
                
                UpdateTracker(tuples.Select(Selector)
                .Aggregate(new Dictionary<string, long>(), (map, next) =>
                {
                    map[next.Item1] = next.Item2;
                    return map;
                }));
                OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }


        }

        private static long GetTracker(Dictionary<string, long> changes, string key)
        {
            long value;
            if (changes.TryGetValue(key, out value))
                return value;
            return -1;
        }

        private void UpdateTracker(Dictionary<string, long> changes)
        {
            lock (padlock)
            {
                if (!string.IsNullOrEmpty(cachePath))
                {
                    new SimpleDictionaryWriter().Write(cachePath, changes);
                }
            }
        }

        private Dictionary<string, long> InitializeFromTracker()
        {
            lock (padlock)
            {
                return new SimpleDictionaryWriter().Read(cachePath);
            }
        }

        private Tuple<string, long> Selector(Tuple<string, IStorageChanges> tuple)
        {
            var changes = tuple.Item2;
            index
                .WriteAll(changes.Created)
                .WriteAll(changes.Updated)
                .DeleteAll(changes.Deleted);


            return new Tuple<string, long>(tuple.Item1, changes.Token);
        }

        public void QueueUpdate(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Write(entity);

            //Note: This will cause the callback to get called right away...
            callback.Signal();
        }

        public void QueueDelete(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Delete(entity);

            callback.Signal();
        }

        protected virtual void OnIndexChanged(IndexChangesEventArgs args)
        {
            var handler = IndexChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }

    public interface IScheduler
    {
        IScheduledTask Schedule(IScheduledTask task);
        IScheduledTask Schedule(Action<bool> callback, TimeSpan period);
    }

    public interface IScheduledTask
    {
    }

    public class ScheduledTask : IScheduledTask
    {
    }
    
    public class Scheduler : IDisposable
    {
        private readonly Action<bool> callback;
        private readonly TimeSpan period;
        private readonly AutoResetEvent handle = new AutoResetEvent(false);
        private bool disposed = false;

        public Scheduler(Action<bool> callback, TimeSpan period)
        {
            this.callback = callback;
            this.period = period;
            Next();
        }

        private void Next()
        {
            ThreadPool.RegisterWaitForSingleObject(handle, ExecuteCallback, null, period, true);
        }

        private void ExecuteCallback(object state, bool timedout)
        {
            if (disposed)
                return;

            callback(!timedout);
            Next();
        }

        public void Dispose()
        {
            disposed = true;
            Signal();
        }

        public void Signal()
        {
            handle.Set();
        }
    }
}
