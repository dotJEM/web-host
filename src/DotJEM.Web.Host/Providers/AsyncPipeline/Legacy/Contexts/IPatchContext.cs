using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Contexts
{
    public interface IPatchContext : IContext
    {
        JObject Previous { get; }
    }
}