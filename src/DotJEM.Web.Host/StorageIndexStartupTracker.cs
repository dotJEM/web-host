using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

namespace DotJEM.Web.Host;

public class StorageIndexStartupTracker : IObserver<IInfoStreamEvent>
{
    private readonly IInitializationTracker initialization;
    private readonly ConcurrentDictionary<string, IngestState> ingestStates = new();
    private readonly RestoreState restoreState = new RestoreState();
    private readonly IngestState ingestState = new IngestState();

    public StorageIndexStartupTracker(IInitializationTracker initialization)
    {
        this.initialization = initialization;
    }

    public void OnNext(IInfoStreamEvent value)
    {
        switch (value)
        {
            case IndexStartingEvent indexStartingEvent:
                ingestState.Reset(indexStartingEvent);
                initialization.SetProgress(ingestState.ToString());
                break;
            case IndexIngestEvent indexIngestEvent:
                ingestState.Increment(indexIngestEvent);
                initialization.SetProgress(ingestState.ToString());
                break;
            case IndexInitializedEvent indexInitializedEvent:
                ingestState.Complete(indexInitializedEvent);
                initialization.SetProgress(ingestState.ToString());
                break;
            case ZipSnapshotEvent { Level: "OPEN" } zipSnapshotEvent:
                restoreState.Reset(zipSnapshotEvent.Snapshot);
                initialization.SetProgress(restoreState.ToString());
                break;
            case ZipFileEvent { Level: "OPEN" } zipFileEvent:
                restoreState.StartRestore(zipFileEvent.File);
                initialization.SetProgress(restoreState.ToString());
                break;
            case ZipFileEvent { Level: "CLOSE" } zipFileEvent:
                restoreState.EndRestore(zipFileEvent.File);
                initialization.SetProgress(restoreState.ToString());
                break;
            case ZipSnapshotEvent { Level: "CLOSE" }:
                restoreState.Complete();
                initialization.SetProgress(restoreState.ToString());
                break;
        }
    }

    private class RestoreState
    {
        private readonly ConcurrentDictionary<string, RestoreFileState> files = new();

        public void Reset(LuceneZipSnapshot snapshot)
        {
            files[snapshot.SegmentsFile.Name] = new RestoreFileState(snapshot.SegmentsFile.Name);
            foreach (ILuceneFile file in snapshot.Files)
                files[file.Name] = new RestoreFileState(file.Name);
        }

        public void Complete()
        {

        }

        public void StartRestore(LuceneZipFile file)
        {
            files.AddOrUpdate(file.Name, s => new RestoreFileState(s), (s, state) => state.Start());
        }

        public void EndRestore(LuceneZipFile file)
        {
            files.AddOrUpdate(file.Name, s => new RestoreFileState(s), (s, state) => state.Complete());
        }

        public override string ToString()
        {
            return files.Values
                .Aggregate(new StringBuilder("Restoring index:").AppendLine(), (builder, state) => builder.AppendLine(state.ToString()))
                .ToString();
        }

        private class RestoreFileState
        {
            private enum State { Waiting, Restoring, Restored }

            private readonly string fileName;
            private State state = State.Waiting;

            public RestoreFileState(string fileName)
            {
                this.fileName = fileName;
            }

            public RestoreFileState Start()
            {
                state = State.Restoring;
                return this;
            }

            public RestoreFileState Complete()
            {
                state = State.Restored;
                return this;
            }
            public override string ToString()
                => $" -> {fileName}: {state}";
        }

    }

    private class IngestState
    {
        private ChangeCount count;
        private readonly Stopwatch timer = Stopwatch.StartNew();
        private readonly ConcurrentDictionary<string, AreaState> areaStates = new();

        public void Reset(IndexStartingEvent indexStartingEvent)
        {
            areaStates.AddOrUpdate(indexStartingEvent.Area,
                s => new AreaState(s, indexStartingEvent.InitialGeneration, indexStartingEvent.LatestGeneration),
                (s, _) => new AreaState(s, indexStartingEvent.InitialGeneration, indexStartingEvent.LatestGeneration));
        }

        public void Increment(IndexIngestEvent indexIngestEvent)
        {
            lock (timer) count += indexIngestEvent.Changes.Count;

            areaStates.AddOrUpdate(indexIngestEvent.Changes.StorageArea,
                s => new AreaState(s).Increment(indexIngestEvent.Changes),
                (s, state) => state.Increment(indexIngestEvent.Changes));
        }

        public void Complete(IndexInitializedEvent indexInitializedEvent)
        {
            areaStates.AddOrUpdate(indexInitializedEvent.Area,
                s => new AreaState(s).Complete(indexInitializedEvent.Generation),
                (s, state) => state.Complete(indexInitializedEvent.Generation));

            if (areaStates.Values.All(s => s.Done))
                this.timer.Stop();
        }

        public override string ToString()
        {
            return areaStates.Values
                .Aggregate(new StringBuilder($"[{timer.Elapsed:c}] Ingesting data storage: {count.Total} ({(int)(count.Total / timer.Elapsed.TotalSeconds)}/sec)").AppendLine(),
                    (builder, state) => builder.AppendLine(state.ToString()))
                .ToString();
        }

        private class AreaState
        {
            private readonly string area;
            private readonly Stopwatch timer = Stopwatch.StartNew();
            private readonly long initialGeneration;
            private readonly long latestGeneration;

            private long generation;
            private ChangeCount count;
            public bool Done { get; private set; }

            public AreaState(string area, long initialGeneration = 0, long latestGeneration = 0)
            {
                this.area = area;
                this.initialGeneration = initialGeneration;
                this.latestGeneration = latestGeneration;
            }

            public AreaState Increment(IStorageChangeCollection changes)
            {
                generation = changes.Generation;
                count += changes.Count;
                return this;
            }

            public AreaState Complete(long generation)
            {
                this.Done = true;
                this.generation = generation;
                timer.Stop();
                return this;
            }

            public override string ToString()
                => $" -> {area}: {generation} / {latestGeneration} changes processed, {count.Total} objects indexed. {(Done ? "Completed" : "Indexing")} " +
                   $"({(int)(count.Total / timer.Elapsed.TotalSeconds)}/sec)";
        }
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }
}