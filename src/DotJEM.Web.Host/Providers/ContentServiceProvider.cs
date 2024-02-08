using DotJEM.Diagnostic;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Providers.Data;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers.Services.DiffMerge;

namespace DotJEM.Web.Host.Providers;

public class ContentServiceProvider : ServiceProvider<IContentService>
{
    private readonly IStorageContext context;

    public ContentServiceProvider(IJsonIndex index, IStorageContext context, IDataStorageManager manager, IPipeline pipeline, IJsonMergeVisitor merger, ILogger performance)
        : base(name => new ContentService(index, context.Area(name), manager, pipeline, merger, performance))
    {
        this.context = context;
    }

    public override bool Release(string areaName)
    {
        return base.Release(areaName) && context.Release(areaName);
    }
}