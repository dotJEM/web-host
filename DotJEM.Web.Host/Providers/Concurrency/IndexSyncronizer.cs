using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Tasks;
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
        event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        event EventHandler<IndexChangesEventArgs> IndexChanged;

        void Start();
        void Stop();
        void UpdateIndex();

        void QueueUpdate(JObject entity);
        void QueueDelete(JObject entity);
    }

    public class IndexInitializedEventArgs : EventArgs
    {

        public IndexInitializedEventArgs()
        {
        }
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
        public event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        public event EventHandler<IndexChangesEventArgs> IndexChanged;

        private readonly IStorageIndex index;
        private readonly IWebScheduler scheduler;
        private readonly IInitializationTracker tracker;

        private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly TimeSpan interval;

        private IScheduledTask task;

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler, IInitializationTracker tracker)
        {
            this.index = index;
            this.scheduler = scheduler;
            this.tracker = tracker;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);

            if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
                buffer = (int)AdvConvert.ToByteCount(configuration.Index.Watch.RamBuffer) / (1024 * 1024);

            foreach (WatchElement watch in configuration.Index.Watch.Items)
            {
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
            int total = 0;
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                while (true)
                {
                    IEnumerable<Tuple<string, IStorageChanges>> tuples = logs
                        .Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get(false)))
                        .ToList();

                    int sum = tuples.Sum(t => t.Item2.Count.Total);
                    if (sum < 1)
                        break;

                    //TODO: Using SYNC here is a hack, ideally we would wan't to use a prober Async pattern, but this requires a bigger refactoring.
                    Sync.Await(tuples.Select(tup => InitializeChangesAsync(writer, tup)));

                    //TODO: This is a bit heavy on the load, we would like to wait untill the end instead, but
                    //      if we do that we should either send a "initialized" even that instructs controllers
                    //      and services that the index is now fully ready. Or we neen to collect all data, the later not being possible as it would
                    //OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));

                    total += sum;
                    long tokens = tuples.Sum(t => t.Item2.Token);
                    tracker.SetProgress($"{tokens} changes processed, {total} objects indexed.");
                }
            }
            OnIndexInitialized(new IndexInitializedEventArgs());
        }

        private async Task<long> InitializeChangesAsync(ILuceneWriteContext writer, Tuple<string, IStorageChanges> tuple)
        {
            IStorageChanges changes = tuple.Item2;
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

            // ReSharper disable ReturnValueOfPureMethodIsNotUsed
            //  - TODO: Using SYNC here is a hack, ideally we would wan't to use a prober Async pattern, 
            //          but this requires a bigger refactoring.
            tuples.Select(WriteChanges).ToArray();
            // ReSharper restore ReturnValueOfPureMethodIsNotUsed
            OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));
            index.Flush();

        }

        private long WriteChanges(Tuple<string, IStorageChanges> tuple)
        {
            IStorageChanges changes = tuple.Item2;
            index.WriteAll(changes.Created);
            index.WriteAll(changes.Updated);
            index.WriteAll(changes.Deleted);
            return changes.Token;
        }


        private readonly int buffer = 512;


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

        protected virtual void OnIndexInitialized(IndexInitializedEventArgs e)
        {
            IndexInitialized?.Invoke(this, e);
        }
    }
}
