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
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Util;
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
        private readonly IWebScheduler scheduler;
        private readonly IInitializationTracker tracker;
        private readonly object padlock = new object();

        private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly TimeSpan interval;
        private readonly string cachePath;

        private IScheduledTask task;

        public StorageIndexManager(
            IStorageIndex index,
            IStorageContext storage, 
            IWebHostConfiguration configuration,
            IWebScheduler scheduler,
            IInitializationTracker tracker)
        {
            this.index = index;
            this.scheduler = scheduler;
            this.tracker = tracker;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);
            foreach (WatchElement watch in configuration.Index.Watch.Items){
                logs[watch.Area] = storage.Area(watch.Area).Log;
            }
            //TODO: Use the below to store a index pointer.
            if (!string.IsNullOrEmpty(configuration.Index.CacheLocation)) {
                cachePath = Path.Combine(HostingEnvironment.MapPath(configuration.Index.CacheLocation), "tracker");
            }
        }

        public void Start()
        {
            InitializeIndex();

            task = scheduler.ScheduleTask("ChangeLogWatcher", b => UpdateIndex(), interval);
        }

        public void Stop()
        {
            task.Dispose();
        }

        private void InitializeIndex()
        {
            Dictionary<string, long> tracker = InitializeFromTracker();

            while (true)
            {
                IEnumerable<Tuple<string, IStorageChanges>> tuples = logs
                    .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get(GetTracker(tracker, log.Key))))
                    .ToList();
                
                if (tuples.All(t => t.Item2.Count.Total < 1))
                    return;

                this.tracker.SetProgress(this.tracker.Percent+1);

                tuples.Select(Selector).ForEach(next => tracker[next.Item1] = next.Item2);
            }
        }

        private void UpdateIndex()
        {
            IEnumerable<Tuple<string, IStorageChanges>> tuples = logs
                .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get()))
                .ToList();

            if (tuples.All(t => t.Item2.Count.Total < 1))
            {
                return;
            }

            UpdateTracker(tuples.Select(Selector)
            .Aggregate(new Dictionary<string, long>(), (map, next) =>
            {
                map[next.Item1] = next.Item2;
                return map;
            }));
            OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));
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
            IStorageChanges changes = tuple.Item2;
            index
                .WriteAll(changes.Created)
                .WriteAll(changes.Updated)
                .DeleteAll(changes.Deleted);
            OptimizeIndex(changes.Count);

            return new Tuple<string, long>(tuple.Item1, changes.Token);
        }
        
        private const long CHANGE_OPTIMIZE_CAP = 1024 * 16; // ~16.000 updates before optimize.
        private long changeCounter = CHANGE_OPTIMIZE_CAP;
        private DateTime lastOptimize = DateTime.Now;

        private void OptimizeIndex(long changes)
        {
            changeCounter -= changes;
            if (changeCounter < 0 || (DateTime.Now - lastOptimize) > TimeSpan.FromMinutes(60))
            {
                index.Optimize();
                changeCounter = CHANGE_OPTIMIZE_CAP;
                lastOptimize = DateTime.Now;
            }
        }

        public void QueueUpdate(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Write(entity);

            //Note: This will cause the callback to get called right away...
            task.Signal();
        }

        public void QueueDelete(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Delete(entity);

            task.Signal();
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
}
