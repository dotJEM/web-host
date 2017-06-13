using DotJEM.Json.Storage.Adapter.Materialize.Log;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class StorageIndexChangeLogWatcherInitializationProgress
    {
        public bool Done { get; }
        public string Area { get; }
        public ChangeCount Count { get; }
        public long Token { get; }

        public StorageIndexChangeLogWatcherInitializationProgress(string area, ChangeCount count, long token, bool done)
        {
            Done = done;
            Area = area;
            Count = count;
            Token = token;
        }
    }
}