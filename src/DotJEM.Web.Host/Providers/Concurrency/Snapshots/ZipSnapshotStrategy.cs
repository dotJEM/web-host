using System;
using System.IO;
using System.Linq;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public class ZipSnapshotStrategy : ISnapshotStrategy
{
    private readonly string path;
    
    public IInfoStream InfoStream { get; } = new DefaultInfoStream<ZipSnapshotStrategy>();

    public ZipSnapshotStrategy(string path)
    {
        this.path = path;
    }

    public ISnapshotTarget CreateTarget(JObject metaData)
    {
        return new ZipSnapshotTarget(path, metaData);
    }

    public ISnapshotSourceWithMetadata CreateSource(int offset)
    {
        string[] files = GetSnapshots();
        return files.Length > offset ? new ZipSnapshotSource(files[offset]) : null;
    }

    public void CleanOldSnapshots(int maxSnapshots)
    {
        foreach (string file in GetSnapshots().Skip(maxSnapshots))
        {
            try
            {
                File.Delete(file);
                InfoStream.WriteInfo($"Deleted snapshot: {file}");
            }
            catch (Exception ex)
            {
                InfoStream.WriteError($"Failed to delete snapshot: {file}", ex);
            }
        }
    }

    private string[] GetSnapshots()
    {
        return Directory.GetFiles(path, "*.zip")
            .OrderByDescending(file => file)
            .ToArray();
    }

}