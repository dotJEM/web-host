using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipSnapshotEvent : IInfoStreamEvent
{
    public LuceneZipSnapshot Snapshot { get; }

    public string Level { get; }
    public string Message { get; }

    public ZipSnapshotEvent(LuceneZipSnapshot snapshot, string level)
    {
        Level = level;
        this.Snapshot = snapshot;
    }
}