using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public delegate INext<T> NextFactoryDelegate<T>(IPipelineContext context, INode<T> node);
}