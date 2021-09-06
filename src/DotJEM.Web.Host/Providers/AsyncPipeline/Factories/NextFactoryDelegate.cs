using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public delegate INext NextFactoryDelegate(IPipelineContext context, INode node);
}