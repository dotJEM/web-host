using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Util;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
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
    }

    public interface IStorageIndexManagerInfoStream
    {
        void Track(string area, int creates, int updates, int deletes, int faults);
        void Record(string area, IList<FaultyChange> faults);
        void Publish(IStorageChangeCollection changes);
    }

    //TODO: Not really a stream, but we need some info for now, then we can make it into a true info stream later.
    public class StorageIndexManagerInfoStream : IStorageIndexManagerInfoStream
    {
        private readonly ConcurrentDictionary<string, AreaInfo> areas = new ConcurrentDictionary<string, AreaInfo>();

        public void Track(string area, int creates, int updates, int deletes, int faults)
        {
            AreaInfo info = areas.GetOrAdd(area, s => new AreaInfo(s));
            info.Track(creates, updates, deletes, faults);
        }

        public void Record(string area, IList<FaultyChange> faults)
        {
            AreaInfo info = areas.GetOrAdd(area, s => new AreaInfo(s));
            info.Record(faults);
        }

        public virtual void Publish(IStorageChangeCollection changes)
        {
        }

        public JObject ToJObject()
        {
            JObject json = new JObject();
            long creates = 0, updates = 0, deletes = 0, faults = 0;
            //NOTE: This places them at top which is nice for human readability, machines don't care.
            json["creates"] = creates;
            json["updates"] = updates;
            json["deletes"] = deletes;
            json["faults"] = faults;
            foreach (AreaInfo area in areas.Values)
            {
                creates += area.Creates;
                updates += area.Updates;
                deletes += area.Deletes;
                faults += area.Faults;

                json[area.Area] = area.ToJObject();
            }
            json["creates"] = creates;
            json["updates"] = updates;
            json["deletes"] = deletes;
            json["faults"] = faults;
            return json;
        }

        private class AreaInfo
        {
            private long creates = 0, updates = 0, deletes = 0, faults = 0;
            private readonly ConcurrentBag<FaultyChange> faultyChanges = new ConcurrentBag<FaultyChange>();

            public string Area { get; }

            public long Creates => creates;
            public long Updates => updates;
            public long Deletes => deletes;
            public long Faults => faults;
            public FaultyChange[] FaultyChanges => faultyChanges.ToArray();

            public AreaInfo(string area)
            {
                Area = area;
            }

            public void Track(int creates, int updates, int deletes, int faults)
            {
                Interlocked.Add(ref this.creates, creates);
                Interlocked.Add(ref this.updates, updates);
                Interlocked.Add(ref this.deletes, deletes);
                Interlocked.Add(ref this.faults, faults);
            }

            public void Record(IList<FaultyChange> faults)
            {
                faults.ForEach(faultyChanges.Add);
            }

            public JObject ToJObject()
            {
                JObject json = new JObject();
                json["creates"] = creates;
                json["updates"] = updates;
                json["deletes"] = deletes;
                json["faults"] = faults;
                if (faultyChanges.Any())
                    json["faultyChanges"] = JArray.FromObject(FaultyChanges.Select(c => c.CreateEntity()));
                return json;
            }
        }
    }
    public interface IStorageManager
    {
        void Start();
        void Stop();
    }

    public class StorageManager : IStorageManager
    {
        private IScheduledTask task;
        private readonly IWebScheduler scheduler;
        private readonly Dictionary<string, IStorageHistoryCleaner> cleaners = new Dictionary<string, IStorageHistoryCleaner>();
        private readonly TimeSpan interval;

        public StorageManager(IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler)
        {
            this.scheduler = scheduler;
            this.interval = AdvConvert.ConvertToTimeSpan(configuration.Storage.Interval);
            foreach (StorageAreaElement areaConfig in configuration.Storage.Items)
            {
                IStorageAreaConfigurator areaConfigurator = storage.Configure.Area(areaConfig.Name);
                if (!areaConfig.History)
                    continue;

                areaConfigurator.EnableHistory();
                if (string.IsNullOrEmpty(areaConfig.HistoryAge))
                    continue;

                TimeSpan historyAge = AdvConvert.ConvertToTimeSpan(areaConfig.HistoryAge);
                if(historyAge <= TimeSpan.Zero)
                    continue;

                cleaners.Add(areaConfig.Name, new StorageHistoryCleaner(
                    new Lazy<IStorageAreaHistory>(() => storage.Area(areaConfig.Name).History),
                    historyAge));
            }
        }
        public void Start()
        {
            task = scheduler.ScheduleTask("StorageManager.CleanHistory", b => CleanHistory(), interval);
        }

        private void CleanHistory()
        {
            foreach (IStorageHistoryCleaner cleaner in cleaners.Values)
            {
                cleaner.Execute();
            }
        }

        public void Stop() => task.Dispose();

    }

    public interface IStorageHistoryCleaner
    {
        void Execute();
    }

    public class StorageHistoryCleaner : IStorageHistoryCleaner
    {
        private readonly TimeSpan maxAge;
        private readonly Lazy<IStorageAreaHistory> serviceProvider;

        private IStorageAreaHistory History => serviceProvider.Value;

        public StorageHistoryCleaner(Lazy<IStorageAreaHistory> serviceProvider, TimeSpan maxAge)
        {
            this.serviceProvider = serviceProvider;
            this.maxAge = maxAge;
        }

        public void Execute()
        {
            History.Delete(maxAge);
        }
    }

    public class StorageIndexManager : IStorageIndexManager
    {
        public event EventHandler<IndexResetEventArgs> IndexReset;
        public event EventHandler<IndexInitializedEventArgs> IndexInitialized;
        public event EventHandler<IndexChangesEventArgs> IndexChanged;

        private readonly IStorageIndex index;
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

        //TODO: To many dependencies, refactor!
        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler, IInitializationTracker tracker, IDiagnosticsLogger logger)
        {
            this.index = index;
            this.debugging = configuration.Index.Debugging;
            if (this.debugging)
                this.index.Writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });
            this.scheduler = scheduler;
            this.tracker = tracker;
            this.logger = logger;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);

            if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
                buffer = (int)AdvConvert.ConvertToByteCount(configuration.Index.Watch.RamBuffer) / (1024 * 1024);

            watchers = configuration.Index.Watch.Items
                .ToDictionary(we => we.Area, we => (IStorageIndexChangeLogWatcher) 
                new StorageChangeLogWatcher(we.Area, storage.Area(we.Area).Log, we.BatchSize < 1 ? configuration.Index.Watch.BatchSize : we.BatchSize, logger, InfoStream));
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

                await Task.WhenAll(watchers.Values.Select(watcher => watcher.Reset(writer,0, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(
                    progress => tracker.SetProgress($"{initTracker.Capture(progress)}")))));
            }
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
            //IndexWriter w = index.Storage.GetWriter(index.Analyzer);
            StorageIndexManagerInitializationProgressTracker initTracker = new StorageIndexManagerInitializationProgressTracker(watchers.Keys.Select(k => k));
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                if(debugging)
                    writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new {args});

                Sync.Await(watchers.Values.Select(watcher => watcher.Initialize(writer, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(
                    progress => tracker.SetProgress($"{initTracker.Capture(progress)}")))));
            }
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