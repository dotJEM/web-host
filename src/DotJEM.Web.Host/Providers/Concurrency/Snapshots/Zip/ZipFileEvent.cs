using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipFileEvent : IInfoStreamEvent
{
    private LuceneZipFile file;

    public string Level { get; }
    public string Message { get; }

    public ZipFileEvent(LuceneZipFile file, string level)
    {
        Level = level;
        this.file = file;
    }
}