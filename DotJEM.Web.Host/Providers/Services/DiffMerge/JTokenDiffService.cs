using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge
{
    public interface IJTokenMergeVisitor
    {
        IMergeResult Merge(JToken update, JToken other, JToken origin);
    }

    public interface IJTokenMergeContext
    {
        JToken Origin { get; }
        JToken Merged { get; }

        IMergeResult Noop(JToken update, JToken other);
        IMergeResult Multiple(IEnumerable<MergeResult> diffs, JObject update, JObject other);
        IMergeResult Merge(JToken update, JToken other);

        IJTokenMergeContext Next(object key);
    }

    public class JTokenMergeContext : IJTokenMergeContext
    {
        private readonly object key;
        private readonly JToken parent;

        public JToken Merged { get; }
        public JToken Origin { get; }

        public JTokenMergeContext(JToken merged, JToken origin)
        {
            Merged = merged;
            Origin = origin;
        }

        public JTokenMergeContext(JToken merged, JToken origin, JToken parent, object key)
        {
            Merged = merged;
            Origin = origin;
            this.parent = parent;
            this.key = key;
        }

        public IMergeResult Noop(JToken update, JToken other)
        {
            return new MergeResult(false, update, other, Origin, Merged);
        }

        public IMergeResult Multiple(IEnumerable<MergeResult> diffs, JObject update, JObject other)
        {
            return new CompositeMergeResult(diffs, update, other, Origin, Merged);
        }

        public IMergeResult Merge(JToken update, JToken other)
        {
            if (JToken.DeepEquals(Origin, other))
            {
                //NOTE: The update is valid.
                return new MergeResult(false, update,other,Origin, Merged);
            }

            if (JToken.DeepEquals(Origin, update))
            {
                if (other == null)
                {
                    Merged.Parent.Remove();
                }
                else
                {
                    parent[key] = other;
                }
                return new MergeResult(false, update, other, Origin, Merged);
            }

            //NOTE: Conflict.
            return new MergeResult(true, update, other, Origin, Merged);
        }

        public IJTokenMergeContext Next(object key)
        {
            return new JTokenMergeContext(Merged?[key], Origin?[key], Merged, key);
        }
    }

    public class JTokenMergeVisitor : IJTokenMergeVisitor
    {
        public IMergeResult Merge(JToken update, JToken other, JToken origin)
        {
            return Merge(update, other, new JTokenMergeContext(update.DeepClone(), origin));
        }

        public virtual IMergeResult Merge(JToken update, JToken other, IJTokenMergeContext context)
        {
            //TODO: Test the object for simple value, if it's a simple value we can use DeepEquals right away.

            if (update == null && other == null)
                return context.Noop(null, null);

            if (update == null || other == null || update.Type != other.Type)
                return context.Merge(update, other);
            
            switch (update.Type)
            {
                case JTokenType.Object:
                    return MergeObject((JObject)update, (JObject)other, context);
                case JTokenType.Array:
                    return MergeArray((JArray)update, (JArray)other, context);
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                    return MergeValue((JValue)update, (JValue)other, context);
            }

            throw new ArgumentOutOfRangeException();
        }

        protected virtual IMergeResult MergeValue(JValue update, JValue other, IJTokenMergeContext context)
        {
            return !JToken.DeepEquals(update, other)
                ? context.Merge(update, other)
                : context.Noop(update, other);
        }

        protected virtual IMergeResult MergeObject(JObject update, JObject other, IJTokenMergeContext context)
        {
            IEnumerable<MergeResult> diffs = from key in UnionKeys(update, other)
                let diff = (MergeResult) Merge(update[key], other[key], context.Next(key))
                select diff;
            
            return context.Multiple(diffs, update, other);
        }

        protected virtual IMergeResult MergeArray(JArray update, JArray other, IJTokenMergeContext context)
        {
            if (!JToken.DeepEquals(update, other))
            {
                return context.Merge(update, other);
            }
            return context.Noop(update, other);
        }

        private IEnumerable<string> UnionKeys(IDictionary<string, JToken> update, IDictionary<string, JToken> other)
        {
            HashSet<string> keys = new HashSet<string>(update.Keys);
            keys.UnionWith(other.Keys);
            return keys;
        }
    }

    public class JsonMergeConflictException : Exception
    {
        public MergeResult MergeResult { get; }

        public JsonMergeConflictException(MergeResult result)
        {
            MergeResult = result;
        }

        public JsonMergeConflictException(MergeResult result, string message)
            : base(message)
        {
            MergeResult = result;
        }

        public JsonMergeConflictException(MergeResult result, string message, Exception inner)
            : base(message, inner)
        {
            MergeResult = result;
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
                ["conflict"] = Other,
                ["origin"] = Origin
            }; 
            return diff;
        }
    }


    public class CompositeMergeResult : MergeResult
    {
        private readonly List<MergeResult> diffs;

        public CompositeMergeResult(IEnumerable<MergeResult> diffs, JToken update, JToken other, JToken origin, JToken merged)
            : this(diffs.ToList(), update, other, origin, merged)
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
