using DotJEM.Json.Index.Storage.Snapshot;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public interface ISnapshotSourceWithMetadata : ISnapshotSource
{
    JObject Metadata { get; }
}