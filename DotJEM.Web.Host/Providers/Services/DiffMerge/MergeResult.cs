using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge
{
    public interface IMergeResult
    {
        JToken Update { get; }
        JToken Merged { get; }
        JToken Other { get; }
        JToken Origin { get; }

        JObject Conflicts { get; }

        bool HasConflicts { get; }
    }

    public class MergeResult : IMergeResult
    {
        private readonly JToken merged;
        public JToken Update { get; }

        public JToken Merged
        {
            get
            {
                if (HasConflicts)
                    throw new JsonMergeConflictException(this);

                return merged;
            }
        }

        public JToken Other { get; }
        public JToken Origin { get; }

        public JObject Conflicts => BuildDiff(new JObject(), false);

        public bool HasConflicts { get; }

        //TODO: Store info about the conflict if any
        public MergeResult(bool hasConflicts, JToken update, JToken other, JToken origin, JToken merged)
        {
            this.merged = merged;
            Update = update;
            Other = other;
            Origin = origin;
            HasConflicts = hasConflicts;
        }

        internal virtual JObject BuildDiff(JObject diff, bool includeResolvedConflicts)
        {
            //TODO: (jmd 2015-11-19) Even when there are no conflicts we can still provide a diff, but we need to fix up the Composite first
            if (!HasConflicts && !includeResolvedConflicts)
                return diff;

            //NOTE: Either Merged or Other is not null here, otherwise we would not have a conflict.
            diff[Update?.Path ?? Other.Path] = new JObject
            {
                ["update"] = Update,
                ["other"] = Other,
                ["origin"] = Origin
            }; 
            return diff;
        }
    }

    public class CompositeMergeResult : MergeResult
    {
        private readonly List<MergeResult> diffs;

        public CompositeMergeResult(IEnumerable<MergeResult> diffs, JToken update, JToken other, JToken origin, JToken merged)
            : this((List<MergeResult>)diffs.ToList(), update, other, origin, merged)
        {
        }

        protected CompositeMergeResult(List<MergeResult> diffs, JToken update, JToken other, JToken origin, JToken merged)
            : base(diffs.Any(f => f.HasConflicts), update, other, origin, merged)
        {
            this.diffs = diffs;
        }

        internal override JObject BuildDiff(JObject diff, bool includeResolvedConflicts)
        {
            return diffs.Aggregate(diff, (o, result) => result.BuildDiff(o, includeResolvedConflicts));
        }
    }
}