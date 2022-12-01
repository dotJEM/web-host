using DotJEM.Web.Host.Diagnostics.InfoStreams;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots;

public interface IIndexSnapshotManager
{
    IInfoStream InfoStream { get; }

    void ReplaceStrategy(ISnapshotStrategy strategy);
    bool TakeSnapshot();
    bool RestoreSnapshot();

    void Pause();
    void Resume();
}