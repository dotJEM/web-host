namespace DotJEM.Web.Host.Providers.AsyncPipeline.Contexts
{
    internal class EmptyContext : IGetContext, IPostContext
    {
        public string ContentType { get; }

        public EmptyContext(string contentType)
        {
            ContentType = contentType;
        }
    }
}