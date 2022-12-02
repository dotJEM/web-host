using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public static class ZipInfoStreamExtensions
{
    public static void WriteSnapshotOpenEvent(this IInfoStream self, LuceneZipSnapshot snapshot)
    {
        self.WriteEvent(new ZipSnapshotEvent(snapshot, "OPEN"));
    }

    public static void WriteSnapshotCloseEvent(this IInfoStream self, LuceneZipSnapshot snapshot)
    {
        self.WriteEvent(new ZipSnapshotEvent(snapshot, "CLOSE"));
    }
    public static void WriteFileOpenEvent(this IInfoStream self, LuceneZipFile file)
    {
        self.WriteEvent(new ZipFileEvent(file, "OPEN"));
    }

    public static void WriteFileCloseEvent(this IInfoStream self, LuceneZipFile file)
    {
        self.WriteEvent(new ZipFileEvent(file, "CLOSE"));
    }
}