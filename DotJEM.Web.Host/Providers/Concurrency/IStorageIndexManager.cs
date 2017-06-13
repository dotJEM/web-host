using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Tasks;
using DotJEM.Web.Host.Util;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
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
    public class StorageIndexManager : IStorageIndexManager
    {
        public event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        public event EventHandler<IndexChangesEventArgs> IndexChanged;

        private readonly IStorageIndex index;
        private readonly IWebScheduler scheduler;
        private readonly IInitializationTracker tracker;

        //private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly Dictionary<string, IStorageIndexChangeLogWatcher> watchers;
        private readonly TimeSpan interval;
        private readonly int buffer = 512;

        private IScheduledTask task;

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler, IInitializationTracker tracker)
        {
            this.index = index;
            this.scheduler = scheduler;
            this.tracker = tracker;
            //TODO: This should act as a default batch size.
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);

            if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
                buffer = (int)AdvConvert.ToByteCount(configuration.Index.Watch.RamBuffer) / (1024 * 1024);

            int batchsize = configuration.Index.Watch.BatchSize;
            watchers = configuration.Index.Watch.Items
                .ToDictionary(we => we.Area, we => (IStorageIndexChangeLogWatcher)new StorageChangeLogWatcher(we.Area, storage.Area(we.Area).Log, we.BatchSize < 1 ? batchsize : we.BatchSize));
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
            IndexWriter w = index.Storage.GetWriter(index.Analyzer);
            StorageIndexManagerInitializationProgressTracker initTracker = new StorageIndexManagerInitializationProgressTracker(watchers.Keys.Select(k => k));
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                Sync.Await(watchers.Values.Select(watcher => watcher.Initialize(writer, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(
                    progress => tracker.SetProgress($"{initTracker.Capture(progress)}")))));
            }
            OnIndexInitialized(new IndexInitializedEventArgs());
        }

        public void UpdateIndex()
        {
            IStorageChangeCollection[] changes = Sync.Await(watchers.Values.Select(watcher => watcher.Update(index.Writer)));
            OnIndexChanged(new IndexChangesEventArgs(changes.ToDictionary(c => c.StorageArea)));
            index.Flush();
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

        protected virtual void OnIndexInitialized(IndexInitializedEventArgs e)
        {
            IndexInitialized?.Invoke(this, e);
        }

        public class StorageIndexManagerInitializationProgressTracker
        {
            private readonly ConcurrentDictionary<string, InitializationState> states;

            public StorageIndexManagerInitializationProgressTracker(IEnumerable<string> areas)
            {
                states = new ConcurrentDictionary<string, InitializationState>(areas.ToDictionary(v => v, v => new InitializationState(v)));
            }

            public string Capture(StorageIndexChangeLogWatcherInitializationProgress progress)
            {
                states.AddOrUpdate(progress.Area, s => new InitializationState(s), (s, state) => state.Add(progress.Token, progress.Count, progress.Done));

                return string.Join(Environment.NewLine, states.Values);
            }

            private class InitializationState
            {
                private readonly string area;
                private ChangeCount count;
                private long token;
                private string done;

                public InitializationState(string area)
                {
                    this.area = area;
                }

                public InitializationState Add(long token, ChangeCount count, bool done)
                {
                    this.done = done ? "Completed" : "Indexing";
                    this.count += count;
                    this.token = token;
                    return this;
                }

                public override string ToString() => $" -> {area}: {token} changes processed, {count.Total} objects indexed. {done}";
            }
        }

    }
}