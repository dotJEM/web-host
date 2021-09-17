using System;
using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.Contexts;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Handlers
{
    public abstract class AsyncPipelineHandler : IAsyncPipelineHandler
    {
        public virtual Task<JObject> Get(Guid id, IGetContext context, INextHandler<Guid> next) => next.Invoke();
        public virtual Task<JObject> Post(JObject entity, IPostContext context, INextHandler<JObject> next) => next.Invoke();
        public virtual Task<JObject> Put(Guid id, JObject entity, IPutContext context, INextHandler<Guid, JObject> next) => next.Invoke();
        public virtual Task<JObject> Patch(Guid id, JObject entity, IPatchContext context, INextHandler<Guid, JObject> next) => next.Invoke(id, entity);
        public virtual Task<JObject> Delete(Guid id, IDeleteContext context, INextHandler<Guid> next) => next.Invoke(id);
    }
}