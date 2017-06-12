﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
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
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IStorageIndexManager>().ImplementedBy<StorageIndexManager>().LifestyleSingleton());
        }
    }

    public class StorageIndexChangeLogWatcherInitializationProgress
    {
        public bool Done { get; }
        public string Area { get; }
        public ChangeCount Count { get; }
        public long Token { get; }

        public StorageIndexChangeLogWatcherInitializationProgress(string area, ChangeCount count, long token, bool done)
        {
            Done = done;
            Area = area;
            Count = count;
            Token = token;
        }
    }

    public interface IStorageIndexChangeLogWatcher
    {
        Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null);
    }

    public class StorageChangeLogWatcher : IStorageIndexChangeLogWatcher
    {
        private readonly string area;
        private readonly int batch;
        private readonly IStorageAreaLog log;
        private readonly IStorageIndex index;
        public StorageChangeLogWatcher(string area, IStorageAreaLog log, int batch)
        {
            this.area = area;
            this.log = log;
            this.batch = batch;
        }

        public Task Initialize(ILuceneWriteContext writer, IProgress<StorageIndexChangeLogWatcherInitializationProgress> progress = null)
        {
            progress = progress ?? new Progress<StorageIndexChangeLogWatcherInitializationProgress>();
            return Task.Run(async () =>
            {
                IStorageChangeCollection changes = log.Get(false, batch);
                if (changes.Count < 1)
                {
                    progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Token, true));
                    return;
                }

                //TODO: Check what this implementation does, if it "tolists" it and then writes it then it won't do us any good.
                await writer.CreateAll(changes.Partitioned.Select(change => change.CreateEntity()));
                
                progress.Report(new StorageIndexChangeLogWatcherInitializationProgress(area, changes.Count, changes.Token, false));

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2);
            });
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

    public class IndexInitializedEventArgs : EventArgs { }

    public class IndexChangesEventArgs : EventArgs
    {
        public IDictionary<string, IStorageChangeCollection> Changes { get; }

        public IndexChangesEventArgs(IDictionary<string, IStorageChangeCollection> changes)
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
        private readonly Dictionary<string, IStorageIndexChangeLogWatcher> watchers = new Dictionary<string, IStorageIndexChangeLogWatcher>();
        private readonly TimeSpan interval;

        private IScheduledTask task;

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration, IWebScheduler scheduler, IInitializationTracker tracker)
        {
            this.index = index;
            this.scheduler = scheduler;
            this.tracker = tracker;
            batchsize = configuration.Index.Watch.BatchSize;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);

            if (!string.IsNullOrEmpty(configuration.Index.Watch.RamBuffer))
                buffer = (int)AdvConvert.ToByteCount(configuration.Index.Watch.RamBuffer) / (1024 * 1024);
            foreach (WatchElement watch in configuration.Index.Watch.Items)
            {
                logs[watch.Area] = storage.Area(watch.Area).Log;
                watchers[watch.Area] = new StorageChangeLogWatcher(watch.Area, storage.Area(watch.Area).Log, watch.BatchSize);
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

        private void InitializeIndex()
        {
            IndexWriter w = index.Storage.GetWriter(index.Analyzer);
            StorageIndexManagerInitializationProgressTracker initTracker = new StorageIndexManagerInitializationProgressTracker(watchers.Keys.Select(k => k));

            long processedTokens = 0, loadedObjects = 0, processedObjects = 0;
            using (ILuceneWriteContext writer = index.Writer.WriteContext(buffer))
            {
                Sync.Await(
                    watchers.Values.Select(watcher => watcher.Initialize(writer, new Progress<StorageIndexChangeLogWatcherInitializationProgress>(
                        progress => tracker.SetProgress($"{GetMemmoryStatistics(w)}\n{initTracker.Capture(progress)}")))));

                //foreach (IStorageIndexChangeLogWatcher watcher in watchers.Values)
                //{
                //    Sync.Await(watcher.Initialize(writer, new Progress<object>()));
                //}

                //while (true)
                //{
                //    IEnumerable<Tuple<string, IStorageChangeCollection>> tuples = logs
                //        .Select(log => new Tuple<string, IStorageChangeCollection>(log.Key, log.Value.Get(false, batchsize)))
                //        .ToList();

                //    int sum = tuples.Sum(t => t.Item2.Count.Total);
                //    if (sum < 1)
                //        break;

                //    loadedObjects += sum;
                //    long loadedTokens = tuples.Sum(t => t.Item2.Token);
                //    tracker.SetProgress($"{loadedTokens} changes loaded, {loadedObjects} objects loaded." +
                //                        $"\n{processedTokens} changes processed, {processedObjects} objects indexed." +
                //                        $"\n{GetMemmoryStatistics(w)}");

                //    //TODO: Using SYNC here is a hack, ideally we would wan't to use a prober Async pattern, but this requires a bigger refactoring.
                //    var writerClosure = writer;
                //    Sync.Await(tuples.AsParallel().Select(tup => WriteChangesAsync(writerClosure, tup)));

                //    //TODO: This is a bit heavy on the load, we would like to wait untill the end instead, but
                //    //      if we do that we should either send a "initialized" even that instructs controllers
                //    //      and services that the index is now fully ready. Or we neen to collect all data, the later not being possible as it would
                //    //OnIndexChanged(new IndexChangesEventArgs(tuples.ToDictionary(tup => tup.Item1, tup => tup.Item2)));
                //    processedTokens = loadedTokens;
                //    processedObjects = loadedObjects;
                //    tracker.SetProgress($"{loadedTokens} changes loaded, {loadedObjects} objects loaded." +
                //                        $"\n{processedTokens} changes processed, {processedObjects} objects indexed." +
                //                        $"\n{GetMemmoryStatistics(w)}");
                //    GC.Collect(0);
                //}
            }
            OnIndexInitialized(new IndexInitializedEventArgs());
        }

        private string GetMemmoryStatistics(IndexWriter w)
        {
            return $"Working set: {BytesToString(Environment.WorkingSet)}, GC: {BytesToString(GC.GetTotalMemory(false))}, Lucene: {BytesToString(w.RamSizeInBytes())}";
        }
        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num) + suf[place];
        }

        private async Task<long> WriteChangesAsync(ILuceneWriteContext writer, Tuple<string, IStorageChangeCollection> tuple)
        {
            IStorageChangeCollection changes = tuple.Item2;
            await writer.WriteAll(changes.Created.Select(c => c.CreateEntity()));
            await writer.WriteAll(changes.Updated.Select(c => c.CreateEntity()));
            return changes.Token;
        }

        public void UpdateIndex()
        {

            IEnumerable<Tuple<string, IStorageChangeCollection>> tuples = logs
            .Select(log => new Tuple<string, IStorageChangeCollection>(log.Key, log.Value.Get()))
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

        private long WriteChanges(Tuple<string, IStorageChangeCollection> tuple)
        {
            IStorageChangeCollection changes = tuple.Item2;
            index.WriteAll(changes.Created.Select(c => c.CreateEntity()));
            index.WriteAll(changes.Updated.Select(c => c.CreateEntity()));
            index.DeleteAll(changes.Deleted.Select(c => c.CreateEntity()));
            return changes.Token;
        }


        private readonly int buffer = 512;
        private int batchsize;


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
