using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IContentService
    {
        IStorageArea StorageArea { get; }
        //TODO: Use a Content Result
        IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20);
        IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        JObject Get(Guid id, string contentType);

        JObject Post(string contentType, JObject entity);
        JObject Put(Guid id, string contentType, JObject entity);

        JObject Delete(Guid id, string contentType);
    }

    public interface IContentMergeService
    {
        JObject EnsureMerge(Guid id, JObject entity, JObject prev);
    }

    public class ContentMergeService : IContentMergeService
    {
        private readonly IJsonMergeVisitor merger;
        private readonly IStorageArea area;

        public ContentMergeService(IJsonMergeVisitor merger, IStorageArea area)
        {
            this.merger = merger;
            this.area = area;
        }

        public JObject EnsureMerge(Guid id, JObject update, JObject other)
        {
            //TODO: (jmd 2015-11-25) Dummy for designing the interface. Remove.
            //throw new JsonMergeConflictException(new DummyMergeResult());

            if (!area.HistoryEnabled)
                return update;

            if (update["$version"] == null)
            {
                throw new InvalidOperationException("A $version property is required for all PUT request, it should be the version of the document as you retreived it.");
            }

            int uVersion = (int)update["$version"];
            int oVersion = (int)other["$version"];

            if (uVersion == oVersion)
                return update;

            JObject origin = area.History.Get(id, uVersion);
            return (JObject)merger
                .Merge(update, other, origin)
                .AddVersion(uVersion, oVersion)
                .Merged;
        }
    }

    //TODO: Apply Pipeline for all requests.
    public class ContentService : IContentService
    {
        private readonly IStorageIndex index;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IPipeline pipeline;
        private readonly IContentMergeService merger;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageIndex index, IStorageArea area, IStorageIndexManager manager, IPipeline pipeline, IJsonMergeVisitor merger)
        {
            this.index = index;
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
            this.merger = new ContentMergeService(merger, area);
        }

        public IEnumerable<JObject> Get(string contentType, int skip = 0, int take = 20)
        {
            JObject[] res = index.Search("contentType: " + contentType)
                .Skip(skip).Take(take)
                .Select(hit => hit.Json)
                //Note: Execute the pipeline for each element found
                .Select(json =>
                {
                    using (PipelineContext context = new PipelineContext())
                    {
                        return pipeline.ExecuteAfterGet(json, contentType, context);
                    }
                })
                .Cast<JObject>().ToArray();

            return res;

            //TODO: Execute pipeline for array
            //TODO: Paging and other neat stuff...
        }

        public JObject Get(Guid id, string contentType)
        {
            using (PipelineContext context = new PipelineContext())
            {
                //TODO: Throw exception if not found?
                JObject entity = area.Get(id);
                return pipeline.ExecuteAfterGet(entity, contentType, context);
            }
        }

        public JObject History(Guid id, string contentType, int version)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return null;

            return area.History.Get(id, version);
        }

        public IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Enumerable.Empty<JObject>();

            return area.History.Get(id, from, to);
        }

        public JObject Post(string contentType, JObject entity)
        {
            using (PipelineContext context = new PipelineContext())
            {
                entity = pipeline.ExecuteBeforePost(entity, contentType, context);
                entity = area.Insert(contentType, entity);
                entity = pipeline.ExecuteAfterPost(entity, contentType, context);
                manager.QueueUpdate(entity);
                return entity;
            }
        }

        public JObject Put(Guid id, string contentType, JObject entity)
        {
            using (PipelineContext context = new PipelineContext())
            {
                JObject prev = area.Get(id);

                entity = merger.EnsureMerge(id, entity, prev);

                entity = pipeline.ExecuteBeforePut(entity, prev, contentType, context);
                entity = area.Update(id, entity);
                entity = pipeline.ExecuteAfterPut(entity, prev, contentType, context);
                manager.QueueUpdate(entity);
                return entity;
            }
        }

        public JObject Delete(Guid id, string contentType)
        {
            using (PipelineContext context = new PipelineContext())
            {
                pipeline.ExecuteBeforeDelete(area.Get(id), contentType, context);
                JObject deleted = area.Delete(id);
                //TODO: Throw exception if not found?
                if (deleted == null)
                    return null;

                manager.QueueDelete(deleted);
                return pipeline.ExecuteAfterDelete(deleted, contentType, context);
            }
        }
    }
}