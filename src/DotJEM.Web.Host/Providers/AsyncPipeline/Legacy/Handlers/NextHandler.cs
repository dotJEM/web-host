using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public class NextHandler<TOptArg> : INextHandler<TOptArg>
    {
        private readonly TOptArg arg;
        private readonly Func<TOptArg, Task<JObject>> target;

        public NextHandler(TOptArg arg, Func<TOptArg, Task<JObject>> target)
        {
            this.arg = arg;
            this.target = target;
        }

        public Task<JObject> Invoke() => Invoke(arg);

        public Task<JObject> Invoke(TOptArg newArg) => target.Invoke(newArg);
    }
}