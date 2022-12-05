using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Web.Host.Diagnostics.InfoStreams;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public interface ISnapshotSourceWithMetadata : ISnapshotSource
{
    IInfoStream InfoStream { get; }

    JObject Metadata { get; }

    bool Verify();
}