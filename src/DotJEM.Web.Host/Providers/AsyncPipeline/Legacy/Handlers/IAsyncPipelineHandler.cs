using System;
using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public interface IAsyncPipelineHandler
    {
        Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next);
        Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next);
        Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next);
        Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next);
        Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next);
    }
    public interface INextHandler
    {
        Task<JObject> Invoke();
    }
    public interface INextHandler<in TOptArg> : INextHandler
    {
        Task<JObject> Invoke(TOptArg narg);
    }


    public interface INextHandler<in TOptArg1, in TObtArg2> : INextHandler
    {
        Task<JObject> Invoke(TOptArg1 arg1, TObtArg2 arg2);
    }

    public class NextHandler<TOptArg1, TOptArg2> : INextHandler<TOptArg1, TOptArg2>
    {
        private readonly TOptArg1 arg1;
        private readonly TOptArg2 arg2;
        private readonly Func<TOptArg1, TOptArg2, Task<JObject>> target;

        public NextHandler(TOptArg1 arg1, TOptArg2 arg2, Func<TOptArg1, TOptArg2, Task<JObject>> target)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.target = target;
        }

        public Task<JObject> Invoke() => Invoke(arg1, arg2);

        public Task<JObject> Invoke(TOptArg1 newArg1, TOptArg2 newArg2) => target(newArg1, newArg2);
    }
}