using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.NextHandlers;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Factories
{
    public delegate Task<JObject> PipelineExecutorDelegate(IPipelineContext context, INext next);
}