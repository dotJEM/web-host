using DotJEM.Json.Index2;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers;

public class FileServiceProvider : ServiceProvider<IFileService>
{
    public FileServiceProvider(IStorageContext storage, IJsonIndex index)
        : base(name => new FileService(index, storage.Area(name)))
    {
    }
}