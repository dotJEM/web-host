using System;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
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
}