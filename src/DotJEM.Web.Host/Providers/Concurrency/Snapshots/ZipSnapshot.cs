using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.Storage.Snapshot;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public class ZipSnapshot : ISnapshot
{
    private readonly ZipArchive archive;
    private readonly JObject metadata;

    public long Generation { get; }
    public ILuceneFile SegmentsFile { get; }
    public IEnumerable<ILuceneFile> Files { get; }

    public ZipSnapshot(ZipArchive archive, JObject metadata)
    {
        this.archive = archive;
        this.metadata = metadata;

        Files = new List<ILuceneFile>();
        if (metadata["files"] is JArray arr)
            Files = arr.Select(fileName => new ZipLuceneFile((string)fileName, archive));
        SegmentsFile = new ZipLuceneFile((string)metadata["segmentsFile"], archive);
        Generation = (long)metadata["generation"];
    }
        
    public void Dispose()
    {
        archive.Dispose();
    }

    private class ZipLuceneFile : ILuceneFile
    {
        private readonly ZipArchive archive;
        public string Name { get; }

        public ZipLuceneFile(string fileName, ZipArchive archive)
        {
            this.Name = fileName;
            this.archive = archive;
        }

        public Stream Open()
        {
            return archive.GetEntry(Name)?.Open();
        }

    }
}