using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.Storage;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using Newtonsoft.Json.Linq;
using ILuceneFile = DotJEM.Json.Index.Storage.Snapshot.ILuceneFile;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class LuceneZipSnapshot : ISnapshot
{
    private readonly JObject metadata;
    private readonly IInfoStream infoStream;
    private readonly ZipArchive archive;

    public long Generation { get; }
    public ILuceneFile SegmentsFile { get; }
    public IEnumerable<ILuceneFile> Files { get; }

    public LuceneZipSnapshot(ZipArchive archive, JObject metadata, IInfoStream infoStream)
    {
     
        this.archive = archive;
        this.metadata = metadata;
        this.infoStream = infoStream;

        Files = new List<ILuceneFile>();
        if (metadata["files"] is JArray arr)
            Files = arr.Select(fileName => new LuceneZipFile((string)fileName, archive, infoStream));
        SegmentsFile = new LuceneZipFile((string)metadata["segmentsFile"], archive, infoStream);
        Generation = (long)metadata["generation"];
        infoStream.WriteSnapshotOpenEvent(this);
    }
        
    public void Dispose()
    {
        archive.Dispose();
        infoStream.WriteSnapshotCloseEvent(this);
    }

}