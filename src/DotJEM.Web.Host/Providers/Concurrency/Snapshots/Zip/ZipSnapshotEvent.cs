using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipSnapshotEvent : IInfoStreamEvent
{
    private LuceneZipSnapshot snapshot;

    public string Level { get; }
    public string Message { get; }

    public ZipSnapshotEvent(LuceneZipSnapshot snapshot, string level)
    {
        Level = level;
        this.snapshot = snapshot;
    }
}