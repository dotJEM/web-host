using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge
{
    public static class MergeResultExtensions
    {
        public static IMergeResult AddVersion(this IMergeResult self, long yourVersion, long otherVersion)
        {
            return new MergeResultWithVersion((MergeResult) self, yourVersion, otherVersion);
        }
    }

    public interface IMergeResult
    {
        JToken Update { get; }
        JToken Merged { get; }
        JToken Other { get; }
        JToken Origin { get; }

        JObject Conflicts { get; }

        bool HasConflicts { get; }
    }

    public class MergeResultWithVersion : IMergeResult
    {
        private readonly MergeResult inner;

        private readonly long originVersion;
        private readonly long otherVersion;

        public JObject Conflicts => BuildDiff(new JObject(), false);
        public bool HasConflicts => inner.HasConflicts;
        public JToken Origin => inner.Origin;
        public JToken Other => inner.Other;
        public JToken Update => inner.Update;

        public JToken Merged
        {
            get
            {
                if (HasConflicts)
                    throw new JsonMergeConflictException(this);

                return inner.Merged;
            }
        }

        public MergeResultWithVersion(MergeResult inner, long originVersion, long otherVersion)
        {
            this.inner = inner;
            this.originVersion = originVersion;
            this.otherVersion = otherVersion;
        }

        private JObject BuildDiff(JObject diff, bool includeResolvedConflicts)
        {
            diff["$version"] = new JObject
            {
                ["update"] = $"{originVersion}++",
                ["other"] = otherVersion,
                ["origin"] = originVersion
            };
            inner.BuildDiff(diff, includeResolvedConflicts);
            return diff;
        }

        public override string ToString()
        {
            if (HasConflicts)
            {
                return $"{inner} Latest version was {otherVersion}, update was at version {otherVersion}++.";
            }
            return $"{inner}, version is {otherVersion+1}";
        }
    }


    public class MergeResult : IMergeResult
    {
        private readonly JToken merged;

        public JToken Update { get; }
        public JToken Other { get; }
        public JToken Origin { get; }

        public JToken Merged
        {
            get
            {
                if (HasConflicts)
                    throw new JsonMergeConflictException(this);

                return merged;
            }
        }


        public bool HasConflicts { get; }
        public JObject Conflicts => BuildDiff(new JObject(), false);

        private string Path => Update?.Path ?? Other?.Path ?? Origin.Path;

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
            diff[Path] = new JObject
            {
                ["update"] = Update,
                ["other"] = Other,
                ["origin"] = Origin
            }; 
            return diff;
        }

        public override string ToString()
        {
            if (HasConflicts)
            {
                return $"{Path} is conflicted, updated was '{Update}', other was '{Other}' and origin was '{Origin}'.";
            }
            return $"{Path} was not conflicted, value is '{merged}'.";
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

        public override string ToString()
        {
            if (HasConflicts)
            {
                return $"{diffs.Count(m => m.HasConflicts)} was detected, first conflict was {diffs.FirstOrDefault(f => f.HasConflicts)}";
            }
            return $"{diffs.Count} merge results without conflicts.";
        }
    }
}