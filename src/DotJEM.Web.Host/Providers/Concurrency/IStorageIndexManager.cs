using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Util;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Json.Storage.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Concurrency.Snapshots;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Scheduler.Tasks;
using DotJEM.Web.Host.Tasks;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency;

public interface IStorageIndexManager
{
    IDictionary<string, long> Generations { get; }
    IInfoStream InfoStream { get; }

    Task Generation(string area, long gen);

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

    private readonly Dictionary<string, IStorageIndexChangeLogWatcher> watchers;
    private readonly TimeSpan interval;
    private readonly int buffer = 512;

    public IDictionary<string, long> Generations => watchers.ToDictionary(k => k.Key, k => k.Value.Generation);
    public IInfoStream InfoStream { get; } = new DefaultInfoStream<StorageIndexManager>();

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

        snapshot.InfoStream.Forward(InfoStream);

        if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
            buffer = (int)((long)AdvConvert.ConvertToByteCount(configuration.Index.Watch.RamBuffer) / 1.MegaBytes());
            
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

        IStorageIndexChangeLogWatcher CreateChangeLogWatcher(string area, WatchElement we) {
            int batchSize = we.BatchSize < 1 ? configuration.Index.Watch.BatchSize : we.BatchSize;
            StorageChangeLogWatcher watcher = new(area, storage.Area(area).Log, batchSize, we.InitialGeneration, cutoff);
            watcher.InfoStream.Forward(InfoStream);
            return watcher;
        }
    }
        
    public async Task ResetIndex()
    {
        Stop();
        index.Storage.Purge();
        snapshot.Pause();
        using (ILuceneWriteContext writer = index.Writer.WriteContext(new StorageIndexManagerLuceneWriteContextSettings(buffer)))
        {
            if (debugging) writer.InfoEvent += (_, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });
            // ReSharper disable once AccessToDisposedClosure - We are awaiting all so Initialize has done its thing when we continue.
            await Task.WhenAll(watchers.Values.Select(x => x.Initialize(writer, false))).ConfigureAwait(false);
        }
        this.snapshot.TakeSnapshot();
        snapshot.Resume();

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
        snapshot.Pause();
        bool restoredFromSnapshot = this.snapshot.RestoreSnapshot();
        using (ILuceneWriteContext writer = index.Writer.WriteContext(new StorageIndexManagerLuceneWriteContextSettings(buffer)))
        {
            if (debugging)
                writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });

            // ReSharper disable once AccessToDisposedClosure - Sync blocks until these are done.
            Sync.Await(watchers.Values.Select(x => x.Initialize(writer, restoredFromSnapshot)));
        }
        if (!restoredFromSnapshot) this.snapshot.TakeSnapshot();
        snapshot.Resume();
        OnIndexInitialized(new IndexInitializedEventArgs());
    }

    public async Task Generation(string area, long gen)
    {
        if(!watchers.ContainsKey(area))
            return;

        using ILuceneWriteContext writer = index.Writer.WriteContext(new StorageIndexManagerLuceneWriteContextSettings(buffer));
        if (debugging)
            writer.InfoEvent += (sender, args) => logger.Log("indexdebug", Severity.Critical, args.Message, new { args });
        await watchers[area].Reset(writer, gen);
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

    private class StorageIndexManagerLuceneWriteContextSettings : ILuceneWriteContextSettings
    {
        public double BufferSize { get; }

        public StorageIndexManagerLuceneWriteContextSettings(double bufferSize)
        {
            BufferSize = bufferSize;
        }

        private int offset = 0;

        public void AfterWrite(IndexWriter writer, LuceneStorageIndex index, int counterValue)
        {
            bool callCommit = false;
            lock (this)
            {
                // ReSharper disable once AssignmentInConditionalExpression - Done intentionally
                if (callCommit = (offset += counterValue) > 50000)
                    offset = 0;
            }

            if(callCommit) writer.Commit();
        }

    }
}