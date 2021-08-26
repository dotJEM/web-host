using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Contexts
{
    public interface IPutContext : IContext
    {
        JObject Previous { get; }
    }
}