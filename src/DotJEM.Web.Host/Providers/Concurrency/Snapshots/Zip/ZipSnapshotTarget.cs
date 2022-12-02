using System;
using System.IO;
using DotJEM.Json.Index.Storage.Snapshot;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots.Zip;

public class ZipSnapshotTarget : ISnapshotTarget
{
    private readonly string path;
    private readonly JObject metaData;

    public ZipSnapshotTarget(string path, JObject metaData)
    {
        this.path = path;
        this.metaData = metaData;
    }

    public ISnapshotWriter Open(IndexCommit commit)
    {
        return new ZipSnapshotWriter(metaData, Path.Combine(path, $"{DateTime.Now:yyyy-MM-ddTHHmmss}.{commit.Generation:D8}.zip"))
            .WriteMetaData(commit);
    }
}