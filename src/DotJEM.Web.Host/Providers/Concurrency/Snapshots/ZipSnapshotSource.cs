using System.IO;
using System.IO.Compression;
using DotJEM.Json.Index.Storage.Snapshot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public class ZipSnapshotSource : ISnapshotSourceWithMetadata
{
    private readonly ZipArchive archive;

    public JObject Metadata { get; }

    public ZipSnapshotSource(string file)
    {
        archive = ZipFile.Open(file, ZipArchiveMode.Read);
        using Stream metaStream = archive.GetEntry("metadata.json")?.Open();
        using JsonReader reader = new JsonTextReader(new StreamReader(metaStream));
        Metadata = JObject.Load(reader);
    }

    public ISnapshot Open()
    {
        return new ZipSnapshot(archive, Metadata);
    }
}