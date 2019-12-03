using DotJEM.Json.Storage.Adapter.Materialize.Log;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class StorageIndexChangeLogWatcherInitializationProgress
    {
        public bool Done { get; }
        public string Area { get; }
        public ChangeCount Count { get; }
        public long Token { get; }
        public long Latest { get; }

        public StorageIndexChangeLogWatcherInitializationProgress(string area, ChangeCount count, long token, long latest, bool done)
        {
            Done = done;
            Area = area;
            Count = count;
            Token = token;
            Latest = latest;
        }
    }
}