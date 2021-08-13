using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public interface INextHandler<in TOptArg>
    {
        Task<JObject> Invoke();
        Task<JObject> Invoke(TOptArg narg);
    }
}