using DotJEM.Diagnostic;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Providers.Services.DiffMerge;

namespace DotJEM.Web.Host.Providers;

public class ContentServiceProvider : ServiceProvider<IContentService>
{
    private readonly IStorageContext context;

    public ContentServiceProvider(IStorageIndex index, IStorageContext context, IStorageIndexManager manager, IPipeline pipeline, IJsonMergeVisitor merger, ILogger performance)
        : base(name => new ContentService(index, context.Area(name), manager, pipeline, merger, performance))
    {
        this.context = context;
    }

    public override bool Release(string areaName)
    {
        return base.Release(areaName) && context.Release(areaName);
    }
}