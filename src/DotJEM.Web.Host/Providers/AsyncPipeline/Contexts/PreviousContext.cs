using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Contexts
{
    internal class PreviousContext : IPutContext, IPatchContext, IDeleteContext
    {
        public string ContentType { get; }
        public JObject Previous { get; }

        public PreviousContext(string contentType, JObject previous)
        {
            ContentType = contentType;
            Previous = previous;
        }
    }
}