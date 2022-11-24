using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Util;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Concurrency.Snapshots;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Tasks;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public interface IStorageIndexManager
    {
        IDictionary<string, long> Generations { get; }
        IStorageIndexManagerInfoStream InfoStream { get; }

        Task Generation(string area, long gen, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);

        event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        event EventHandler<IndexChangesEventArgs> IndexChanged;
        event EventHandler<IndexResetEventArgs> IndexReset;

        void Start();
        void Stop();
        void UpdateIndex();

        void QueueUpdate(JObject entity);
        void QueueDelete(JObject entity);

        Task ResetIndex();
        JObject CheckIndex(string area, string contentType, Guid id, Func<string, Guid, JObject> ghostFactory);
    }
    
    public class StorageIndexManager : IStorageIndexManager
    {
        public event EventHandler<IndexResetEventArgs> IndexReset;
        public event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        public event EventHandler<IndexChangesEventArgs> IndexChanged;

        private readonly IStorageIndex index;
        private readonly IStorageContext storage;
        private readonly IWebScheduler scheduler;
        private readonly IInitializationTracker tracker;
        private readonly IDiagnosticsLogger logger;

        //private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly Dictionary<string, IStorageIndexChangeLogWatcher> watchers;
        private readonly TimeSpan interval;
        private readonly int buffer = 512;

        public IDictionary<string, long> Generations => watchers.ToDictionary(k => k.Key, k => k.Value.Generation);
        public IStorageIndexManagerInfoStream InfoStream { get; } = new StorageIndexManagerInfoStream();

        private IScheduledTask task;
        private readonly bool debugging;
        private readonly IIndexSnapshotManager snapshot;

        //TODO: To many dependencies, refactor!
        public StorageIndexManager(
            IStorageIndex index, 
            IStorageContext storage,
            IStorageCutoff cutoff,
            IWebHostConfiguration configuration, 
            IWebScheduler scheduler, 
            IInitializationTracker tracker,
            IIndexSnapshotManager snapshot,
            IDiagnosticsLogger logger)
        {
            this.index = index;
            this.storage = storage;
            this.debugging = configuration.Index.Debugging.Enabled;
            if (this.debugging)
                this.index.Writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });
            this.scheduler = scheduler;
            this.tracker = tracker;
            this.snapshot = snapshot;
            this.logger = logger;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);

            if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
                buffer = (int)AdvConvert.ConvertToByteCount(configuration.Index.Watch.RamBuffer) / (1024 * 1024);
            
            WatchElement starWatch = configuration.Index.Watch.Items.FirstOrDefault(w => w.Area == "*");
            if (starWatch != null)
            {
                watchers = storage.AreaInfos
                    .ToDictionary(info => info.Name, info => CreateChangeLogWatcher(info.Name, starWatch));
            }
            else
            {
                watchers = configuration.Index.Watch.Items
                        .ToDictionary(we => we.Area, we => CreateChangeLogWatcher(we.Area, we));
            }

            //watchers = configuration.Index.Watch.Items
            //    .ToDictionary(we => we.Area, we => (IStorageIndexChangeLogWatcher) 
            //    new StorageChangeLogWatcher(we.Area,
            //        storage.Area(we.Area).Log, we.BatchSize < 1 ? configuration.Index.Watch.BatchSize : we.BatchSize, we.InitialGeneration, cutoff, logger, InfoStream));

            IStorageIndexChangeLogWatcher CreateChangeLogWatcher(string area, WatchElement we) {
                int batchSize = we.BatchSize < 1 ? configuration.Index.Watch.BatchSize : we.BatchSize;
                return new StorageChangeLogWatcher(area, storage.Area(area).Log, batchSize, we.InitialGeneration, cutoff, logger, InfoStream);
            }
        }
        
        public async Task ResetIndex()
        {
            Stop();
            index.Storage.Purge();

            StorageIndexManagerInitializationProgressTracker initTracker = new StorageIndexManagerInitializationProgressTracker(watchers.Keys.Select(k => k));
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                if (debugging)
                    writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });

                await Task.WhenAll(watchers.Values.Select(watcher => watcher
                    .Reset(writer, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(progress => tracker
                        .SetProgress($"{initTracker.Capture(progress)}")))));
            }
            this.snapshot.TakeSnapshot();

            OnIndexReset(new IndexResetEventArgs());
            task = scheduler.ScheduleTask("ChangeLogWatcher", b => UpdateIndex(), interval);
        }

        public void Start()
        {
            InitializeIndex();
            task = scheduler.ScheduleTask("ChangeLogWatcher", b => UpdateIndex(), interval);
        }

        public void Stop() => task.Dispose();

        private void InitializeIndex()
        {
            bool restoredFromSnapshot = this.snapshot.RestoreSnapshot();
            StorageIndexManagerInitializationProgressTracker initTracker = new StorageIndexManagerInitializationProgressTracker(watchers.Keys.Select(k => k));
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                if(debugging)
                    writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new {args});

                Sync.Await(watchers.Values.Select(watcher => watcher
                    .Initialize(writer, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(
                    progress => tracker.SetProgress($"{initTracker.Capture(progress)}")))));
            }
            if(!restoredFromSnapshot) this.snapshot.TakeSnapshot();
            OnIndexInitialized(new IndexInitializedEventArgs());
        }

        public async Task Generation(string area, long gen, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            if(!watchers.ContainsKey(area))
                return;
            
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                if (debugging)
                    writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });

                await watchers[area].Reset(writer, gen, progress);
            }
        }

        public void UpdateIndex()
        {
            IStorageChangeCollection[] changes = Sync
                .Await(watchers.Values.Select(watcher => watcher.Update(index.Writer)));
            OnIndexChanged(new IndexChangesEventArgs(changes.ToDictionary(c => c.StorageArea)));
            index.Flush();
        }

        public JObject CheckIndex(string area, string contentType, Guid id, Func<string, Guid, JObject> ghostFactory)
        {
            JObject json = storage.Area(area).Get(id);
            if (json == null)
            {
                JObject ghost = ghostFactory(contentType, id);
                QueueDelete(ghost);
                return new JObject { ["message"] = "Requested object did not exist in the database, queues delete in index." };
            }
            else
            {
                QueueUpdate(json);
                return new JObject { ["message"] = "Requested object force queued for update." };

            }
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

        protected virtual void OnIndexReset(IndexResetEventArgs e)
        {
            IndexReset?.Invoke(this, e);
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
                states.AddOrUpdate(progress.Area, s => new InitializationState(s), (s, state) => state.Add(progress.Token, progress.Latest, progress.Count,  progress.Done));

                return string.Join(Environment.NewLine, states.Values);
            }

            private class InitializationState
            {
                private readonly string area;
                private ChangeCount count;
                private long token;
                private long latest;
                private string done;

                public InitializationState(string area)
                {
                    this.area = area;
                }

                public InitializationState Add(long token, long latest, ChangeCount count, bool done)
                {
                    this.done = done ? "Completed" : "Indexing";
                    this.count += count;
                    this.token = token;
                    this.latest = latest;
                    return this;
                }

                public override string ToString() => $" -> {area}: {token} / {latest} changes processed, {count.Total} objects indexed. {done}";
            }
        }

    }
}