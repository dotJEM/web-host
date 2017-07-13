using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Diagnostics.Performance;
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
        JObject Get(Guid id, string contentType);

        JObject Post(string contentType, JObject entity);
        JObject Put(Guid id, string contentType, JObject entity);

        JObject Delete(Guid id, string contentType);

        IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null);
        JObject History(Guid id, string contentType, int version);
        JObject Revert(Guid id, string contentType, int version);
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
        private const string TRACK_TYPE = "content";

        private readonly IStorageIndex index;
        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;
        private readonly IPipeline pipeline;
        private readonly IPerformanceLogger performance;
        private readonly IContentMergeService merger;

        public IStorageArea StorageArea => area;

        public ContentService(IStorageIndex index, IStorageArea area, IStorageIndexManager manager, IPipeline pipeline, IJsonMergeVisitor merger, IPerformanceLogger performance)
        {
            this.index = index;
            this.area = area;
            this.manager = manager;
            this.pipeline = pipeline;
            this.performance = performance;
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
                    using (PipelineContext context = pipeline.CreateContext(contentType, (JObject)json))
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
            JObject entity = performance.TrackFunction(TRACK_TYPE, () => area.Get(id), $"ContentService.Get({id}, {contentType})");
            using (PipelineContext context = pipeline.CreateContext(contentType, entity))
            {
                //TODO: Throw exception if not found?
                return pipeline.ExecuteAfterGet(entity, contentType, context);
            }
        }

        public JObject History(Guid id, string contentType, int version)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return null;

            return performance.TrackFunction(TRACK_TYPE, () => area.History.Get(id, version), $"ContentService.History({id}, {contentType}, {version})");
        }
        
        public IEnumerable<JObject> History(Guid id, string contentType, DateTime? from = null, DateTime? to = null)
        {
            //TODO: (jmd 2015-11-10) Perhaps we should throw an exception instead (The API already does that btw). 
            if (!area.HistoryEnabled)
                return Enumerable.Empty<JObject>();

            return performance.TrackFunction(TRACK_TYPE, () => area.History.Get(id, from, to), $"ContentService.History({id}, {contentType}, {from}, {to})");
        }

        public JObject Revert(Guid id, string contentType, int version)
        {
            if (!area.HistoryEnabled)
                throw new InvalidOperationException("Cannot revert document when history is not enabled.");

            return performance.TrackFunction(TRACK_TYPE, () =>
            {
                JObject current = area.Get(id);
                using (PipelineContext context = pipeline.CreateContext(contentType, current))
                {
                    JObject entity = area.History.Get(id, version);
                    entity = pipeline.ExecuteBeforeRevert(entity, current, contentType, context);
                    area.Update(id, entity);
                    entity = pipeline.ExecuteAfterRevert(entity, current, contentType, context);
                    manager.QueueUpdate(entity);
                    return entity;
                }
            }, $"ContentService.Revert({id}, {contentType}, {version})");

        }

        public JObject Post(string contentType, JObject entity)
        {
            using (PipelineContext context = pipeline.CreateContext(contentType, entity))
            {
                entity = pipeline.ExecuteBeforePost(entity, contentType, context);
                JObject closure = entity;
                entity = performance.TrackFunction(TRACK_TYPE, () => area.Insert(contentType, closure), $"ContentService.Post({contentType}, $ENTITY)");
                entity = pipeline.ExecuteAfterPost(entity, contentType, context);
                manager.QueueUpdate(entity);
                return entity;
            }
        }

        public JObject Put(Guid id, string contentType, JObject entity)
        {
            using (PipelineContext context = pipeline.CreateContext(contentType, entity))
            {
                JObject prev = area.Get(id);

                entity = merger.EnsureMerge(id, entity, prev);

                entity = pipeline.ExecuteBeforePut(entity, prev, contentType, context);
                JObject closure = entity;
                entity = performance.TrackFunction(TRACK_TYPE, () => area.Update(id, closure), $"ContentService.Put({id},{contentType}, $ENTITY)");
                entity = pipeline.ExecuteAfterPut(entity, prev, contentType, context);
                manager.QueueUpdate(entity);
                return entity;
            }
        }

        public JObject Delete(Guid id, string contentType)
        {
            JObject entity = area.Get(id);
            using (PipelineContext context = pipeline.CreateContext(contentType, entity))
            {
                pipeline.ExecuteBeforeDelete(entity, contentType, context);
                JObject deleted = performance.TrackFunction(TRACK_TYPE, () => area.Delete(id), $"ContentService.Delete({id},{contentType})");
                //TODO: Throw exception if not found?
                if (deleted == null)
                    return null;

                manager.QueueDelete(deleted);
                return pipeline.ExecuteAfterDelete(deleted, contentType, context);
            }
        }

    }

    public static class PipelineExt
    {
        public static PipelineContext CreateContext(this IPipeline self, string contentType, JObject json, [CallerMemberName] string caller = "")
        {
            return self.ContextFactory.Create(caller, contentType, json);
        }
    }
}