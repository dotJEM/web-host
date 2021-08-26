using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Contexts
{
    public interface IDeleteContext : IContext
    {
        JObject Previous { get; }
    }
}