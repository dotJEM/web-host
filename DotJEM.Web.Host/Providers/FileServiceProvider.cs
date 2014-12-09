using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers
{
    public class FileServiceProvider : ServiceProvider<IFileService>
    {
        public FileServiceProvider(IStorageContext storage, IStorageIndex index)
            : base(name => new FileService(index, storage.Area(name)))
        {
        }
    }
}