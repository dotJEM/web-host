using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public interface ISnapshotStrategy
{
    IInfoStream InfoStream { get; }

    ISnapshotTarget CreateTarget(JObject metaData);
    ISnapshotSourceWithMetadata CreateSource(int offset);
    void CleanOldSnapshots(int maxSnapshots);
}