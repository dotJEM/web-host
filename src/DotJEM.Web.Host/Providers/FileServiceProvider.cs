using DotJEM.Diagnostic;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers
{
    public class FileServiceProvider : ServiceProvider<IFileService>
    {
        public FileServiceProvider(IStorageContext storage, IStorageIndex index, ILogger logger, IPerformanceLogAspectSignatureCache cache = null)
            : base(name => new FileService(index, storage.Area(name)), logger, cache)
        {
        }
    }
}