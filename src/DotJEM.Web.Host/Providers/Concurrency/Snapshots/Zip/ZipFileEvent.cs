using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipFileEvent : IInfoStreamEvent
{
    public LuceneZipFile File { get; }

    public string Level { get; }
    public string Message { get; }

    public ZipFileEvent(LuceneZipFile file, string level)
    {
        Level = level;
        this.File = file;
    }
}