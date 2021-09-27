using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IHistoryService
    {
        IStorageArea StorageArea { get; }

        Task<JObject> HistoryAsync(Guid id, string contentType, int version);
        Task<JObject> RevertAsync(Guid id, string contentType, int version);

        Task<IEnumerable<JObject>> HistoryAsync(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<JObject>> DeletedAsync(string contentType, DateTime? from = null, DateTime? to = null);
    }

    public class HistoryService : IHistoryService
    {
        private readonly IPipelines pipelines;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;

        public IStorageArea StorageArea => area;

        public HistoryService(IStorageArea area, IStorageIndexManager manager, IPipelines pipelines)
        {
            this.area = area;
            this.manager = manager;
            this.pipelines = pipelines;
        }

        public Task<JObject> HistoryAsync(Guid id, string contentType, int version)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult((JObject)null);

            return Task.Run(() => area.History.Get(id, version));
        }

        public Task<IEnumerable<JObject>> HistoryAsync(Guid id, string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult(Enumerable.Empty<JObject>());

            return Task.Run(() => area.History.Get(id, from, to));
        }

        public Task<IEnumerable<JObject>> DeletedAsync(string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Task.FromResult(Enumerable.Empty<JObject>());

            return Task.Run(() => area.History.GetDeleted(contentType, from, to));
        }

        public async Task<JObject> RevertAsync(Guid id, string contentType, int version)
        {
            if (!area.HistoryEnabled)
                throw new InvalidOperationException("Cannot revert document when history is not enabled.");

            
            JObject current = area.Get(id);
            JObject target = area.History.Get(id, version);

            RevertContext context = new RevertContext(contentType, id, version, target, current);
            ICompiledPipeline<JObject> pipeline = pipelines
                .For(context, async ctx => area.Update(ctx.Id, context.Target));

            JObject result = await pipeline.Invoke();
            manager.QueueUpdate(result);
            return result;
        }
    }

    public class RevertContext : PipelineContext
    {
        public string ContentType => (string)GetParameter("contentType");
        public Guid Id => (Guid)GetParameter("id");
        public int Version => (int)GetParameter("version");
        public JObject Target => (JObject)GetParameter("target");
        public JObject Current => (JObject)GetParameter("current");

        public RevertContext(string contentType, Guid id, int version, JObject target, JObject current)
        {
            Set("type", "REVERT");
            Set(nameof(contentType), contentType);
            Set(nameof(id), id);
            Set(nameof(version), version);
            Set(nameof(target), target);
            Set(nameof(current), current);
        }
    }

}