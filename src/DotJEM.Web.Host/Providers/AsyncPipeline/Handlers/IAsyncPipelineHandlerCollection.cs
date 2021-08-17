namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public interface IAsyncPipelineHandlerCollection
    {
        IPipeline For(string contentType);
    }
}