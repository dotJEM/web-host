using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge
{
    public interface IJTokenMergeVisitor
    {
        MergeResult Merge(JToken update, JToken other, JToken origin);
    }

    public interface IJTokenMergeContext
    {
        JToken Origin { get; }

        MergeResult TryMerge(JToken update, JToken other);

        IJTokenMergeContext Child(object key, JObject update);
    }

    public class JTokenMergeContext : IJTokenMergeContext
    {
        private readonly object key;
        private readonly JToken parent;
        public JToken Origin { get; }

        public JTokenMergeContext(JToken origin)
        {
            this.Origin = origin;
        }

        public JTokenMergeContext(JToken origin, JToken parent, object key)
        {
            this.Origin = origin;
            this.parent = parent;
            this.key = key;
        }

        public MergeResult TryMerge(JToken update, JToken other)
        {
            if (JToken.DeepEquals(Origin, other))
            {
                //NOTE: The update is valid.
                return new MergeResult(false, update,other,Origin);
            }

            if (JToken.DeepEquals(Origin, update))
            {
                if (other == null)
                {
                    update.Parent.Remove();
                }
                else
                {
                    parent[key] = other;
                }
                return new MergeResult(false, update, other, Origin);
            }

            //NOTE: Conflict.
            return new MergeResult(true, update, other, Origin);
        }

        public IJTokenMergeContext Child(object key, JObject parent)
        {
            return new JTokenMergeContext(Origin?[key], parent, key);
        }
    }

    public class JTokenMergeVisitor : IJTokenMergeVisitor
    {
        public MergeResult Merge(JToken update, JToken other, JToken origin)
        {
            //TODO: (jmd 2015-11-16) Should we merge this into a "cloned" object instead? 
            update = update.DeepClone();
            return Merge(update, other, new JTokenMergeContext(origin));
        }

        public virtual MergeResult Merge(JToken update, JToken other, IJTokenMergeContext context)
        {
            //TODO: Test the object for simple value, if it's a simple value we can use DeepEquals right away.

            if (update == null && other == null)
                return new MergeResult(false, null, null, context.Origin);

            if (update == null || other == null || update.Type != other.Type)
                return context.TryMerge(update, other);

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

        protected virtual MergeResult MergeValue(JValue update, JValue other, IJTokenMergeContext context)
        {
            return !JToken.DeepEquals(update, other) 
                ? context.TryMerge(update, other)
                : new MergeResult(false, update, other, context.Origin);
        }

        protected virtual MergeResult MergeObject(JObject update, JObject other, IJTokenMergeContext context)
        {
            IEnumerable<MergeResult> diffs = from key in UnionKeys(update, other)
                let diff = Merge(update[key], other[key], context.Child(key, update))
                where diff.IsConflict 
                select diff;
            return new CompositeMergeResult(diffs, update, other, context.Origin);
        }

        protected virtual MergeResult MergeArray(JArray update, JArray other, IJTokenMergeContext context)
        {
            if (!JToken.DeepEquals(update, other))
            {
                return context.TryMerge(update, other);
            }
            return new MergeResult(false, update, other, context.Origin);
        }

        private IEnumerable<string> UnionKeys(IDictionary<string, JToken> update, IDictionary<string, JToken> other)
        {
            HashSet<string> keys = new HashSet<string>(update.Keys);
            keys.UnionWith(other.Keys);
            return keys;
        }

    }

    public class MergeResult 
    {
        public JToken Merged { get; }
        public JToken Other { get; }
        public JToken Origin { get; }

        public JObject Diff => BuildDiff(new JObject());

        public bool IsConflict { get; }

        //TODO: Store info about the conflict if any
        public MergeResult(bool isConflict, JToken update, JToken other, JToken origin)
        {
            this.Merged = update;
            this.Other = other;
            this.Origin = origin;
            this.IsConflict = isConflict;
        }

        internal virtual JObject BuildDiff(JObject diff)
        {
            if (!IsConflict)
                return null;
            
            //NOTE: Either Merged or Other is not null here, otherwise we would not have a conflict.
            diff[Merged?.Path ?? Other.Path] = new JObject
            {
                ["updated"] = Merged,
                ["conflict"] = Other,
                ["origin"] = Origin
            }; 
            return diff;
        }

    }


    public class CompositeMergeResult : MergeResult
    {
        private readonly List<MergeResult> diffs;

        protected CompositeMergeResult(List<MergeResult> diffs, JToken update, JToken other, JToken origin) 
            : base(diffs.Any(), update, other, origin)
        {
            this.diffs = diffs;
        }

        public CompositeMergeResult(IEnumerable<MergeResult> diffs, JToken update, JToken other, JToken origin)
            : this(diffs.ToList(), update, other, origin)
        {
        }

        internal override JObject BuildDiff(JObject diff)
        {
            return diffs.Aggregate(diff, (o, result) => result.BuildDiff(o));
        }
    }
}
