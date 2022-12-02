using System.Collections.Generic;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;

namespace DotJEM.Web.Host.Providers.Concurrency;

public interface IStorageIndexManagerInfoStream
{
    void Track(string area, int creates, int updates, int deletes, int faults);
    void Record(string area, IList<FaultyChange> faults);
    void Publish(IStorageChangeCollection changes);
}