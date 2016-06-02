using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        void UpdateIndex();

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

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration,IWebScheduler scheduler, IInitializationTracker tracker)
        {
            this.index = index;
            this.scheduler = scheduler;
            this.tracker = tracker;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);
            foreach (WatchElement watch in configuration.Index.Watch.Items){
                logs[watch.Area] = storage.Area(watch.Area).Log;
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
            //TODO: (jmd 2015-09-24) Build spartial indexes and merge them in the end. 
            // http://lucene.apache.org/core/3_0_3/api/core/org/apache/lucene/index/IndexWriter.html#addIndexesNoOptimize%28org.apache.lucene.store.Directory...%29
            int total = 0;
            using (ILuceneWriteContext writer = index.Writer.WriteContext())
            {
                while (true)
                {
                    IEnumerable<Tuple<string, IStorageChanges>> tuples = logs
                        .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get(false)))
                        .ToList();

                    var sum = tuples.Sum(t => t.Item2.Count.Total);
                    if (sum < 1)
                        break;

                    var executed = tuples.Select(x=>WriteChanges(writer, x).Result).ToList();
                    //TODO: This is a bit heavy on the load, we would like to wait untill the end instead, but
                    //      if we do that we should either send a "initialized" even that instructs controllers
                    //      and services that the index is now fully ready. Or we neen to collect all data, the later not being possible as it would

                    OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));

                    total += sum;
                    var tokens = tuples.Sum(t => t.Item2.Token);
                    this.tracker.SetProgress($"{tokens} changes processed, {total} objects indexed.");
                }
            }
            
            OptimizeIndex(total);
        }

        private async Task<long> WriteChanges(ILuceneWriteContext writer,Tuple<string, IStorageChanges> tuple)
        {
            IStorageChanges changes = tuple.Item2;
            //TODO: We would like to use "CreateAll" here instead as that would probably be faster, but this can cause
            //      duplicates in the index due to changes happening while we index.
            await writer.WriteAll(changes.Created);
            await writer.WriteAll(changes.Updated);
            return changes.Token;
        }

        public void UpdateIndex()
        {
            IEnumerable<Tuple<string, IStorageChanges>> tuples = logs
                .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get()))
                .ToList();

            if (tuples.Sum(t => t.Item2.Count.Total) < 1)
                return;

            var executed = tuples.Select(WriteChangesAndOptimize).ToList();
            OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));
            index.Flush();
        }

        private long WriteChangesAndOptimize(Tuple<string, IStorageChanges> tuple)
        {
            IStorageChanges changes = tuple.Item2;
            index
                .WriteAll(changes.Created)
                .WriteAll(changes.Updated)
                .DeleteAll(changes.Deleted);
            OptimizeIndex(changes.Count);
            return changes.Token;
        }
        
        private const long CHANGE_OPTIMIZE_CAP = 1024 * 16; // ~16.000 updates before optimize.
        private long changeCounter = CHANGE_OPTIMIZE_CAP;
        private DateTime lastOptimize = DateTime.Now;

        private void OptimizeIndex(long changes)
        {
            changeCounter -= changes;
            if (changeCounter >= 0 && (DateTime.Now - lastOptimize) <= TimeSpan.FromMinutes(60))
                return;

            index.Optimize();
            changeCounter = CHANGE_OPTIMIZE_CAP;
            lastOptimize = DateTime.Now;
        }

        public void QueueUpdate(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Write(entity);
            task?.Signal();
        }

        public void QueueDelete(JObject entity)
        {
            //Note: This will cause the entity to be updated in the index twice
            //      but it ensures that the entity is prepared for us if we query it right after this...
            index.Delete(entity);
            task?.Signal();
        }

        protected virtual void OnIndexChanged(IndexChangesEventArgs args)
        {
            IndexChanged?.Invoke(this, args);
        }
    }
}
