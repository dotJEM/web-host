using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipSnapshotSource : ISnapshotSourceWithMetadata
{
    private readonly string file;
    private readonly ZipArchive archive;

    public static ISnapshotSourceWithMetadata Open(string file)
    {
        try
        {
            return new ZipSnapshotSource(file);
        }
        catch
        {
            return new CorruptZipSnapshotSource(file);
        }
    }

    public IInfoStream InfoStream { get; } = new DefaultInfoStream<ZipSnapshotSource>();

    public JObject Metadata { get; }

    private ZipSnapshotSource(string file)
    {
        archive = ZipFile.Open(file, ZipArchiveMode.Read);
        using Stream metaStream = archive.GetEntry("metadata.json")?.Open();
        using JsonReader reader = new JsonTextReader(new StreamReader(metaStream));
        Metadata = JObject.Load(reader);
    }

    public bool Verify()
    {
        string segmentsFile = (string)Metadata["segmentsFile"];
        if (segmentsFile is null)
            return false;

        string[] files = Metadata["files"]?.ToObject<string[]>();
        if(files is null) 
            return false;

        if (archive.GetEntry(segmentsFile) is null)
            return false;

        if (files.Any(file => archive.GetEntry(file) is null))
            return false;

        return true;
    }

    public bool Delete()
    {
        try {
            archive.Dispose();
            File.Delete(file);
            return true;
        }
        catch (Exception e)
        {
            InfoStream.WriteError($"Failed to delete snapshot file: {file}", e);
            return false;
        }
    }

    public ISnapshot Open()
    {
        return new LuceneZipSnapshot(archive, Metadata, InfoStream);
    }

}

public class CorruptZipSnapshotSource : ISnapshotSourceWithMetadata
{
    private readonly string file;
    public IInfoStream InfoStream { get; } = new DefaultInfoStream<ZipSnapshotSource>();
    public JObject Metadata { get; } = new ();

    public CorruptZipSnapshotSource(string file)
    {
        this.file = file;
    }


    public ISnapshot Open()
    {
        throw new InvalidOperationException("Can't open a corrupt snapshot.");
    }

    public bool Verify() => false;

    public bool Delete()
    {
        try {
            File.Delete(file);
            return true;
        }
        catch (Exception e)
        {
            InfoStream.WriteError($"Failed to delete snapshot file: {file}", e);
            return false;
        }
    }
}