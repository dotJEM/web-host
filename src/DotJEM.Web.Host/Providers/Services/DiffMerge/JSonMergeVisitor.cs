using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge;

public interface IJsonMergeVisitor
{
    IMergeResult Merge(JToken update, JToken other, JToken origin);
}

//TODO: (jmd 2015-11-23) MergeVisitor is a bit misguiding here. IT's sort of a diff visitor...
//                       But then again, it handles the merge though the merge context...
//                       - Would like to have that abstracted out, so that the Context would know what to do, and that we could implement
//                         simple difss this way as well. And then allow for different strategies.
public class JsonMergeVisitor : IJsonMergeVisitor
{
    private static readonly HashSet<string> meta = new HashSet<string>("id;contentType;createdBy;updatedBy;$reference;$version;$created;$updated;$schemaVersion;$area"
        .Split(new []{';'},StringSplitOptions.RemoveEmptyEntries));

    public IMergeResult Merge(JToken update, JToken other, JToken origin)
    {
        return Merge(update, other, new JsonMergeContext(update.DeepClone(), origin));
    }

    public virtual IMergeResult Merge(JToken update, JToken other, IJsonMergeContext context)
    {
        if(meta.Contains(update?.Path ?? other?.Path))
            return context.Noop(update, other);

        if (update == null && other == null)
            return context.Noop(null, null);

        if (update == null || other == null || update.Type != other.Type)
            return context.Merge(update, other);

        //TODO: (jmd 2015-11-23) Could change this into try cast into JValue, JObject, JArray -> Might be easier to read.
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

    protected virtual IMergeResult MergeValue(JValue update, JValue other, IJsonMergeContext context)
    {
        return !JToken.DeepEquals(update, other)
            ? context.Merge(update, other)
            : context.Noop(update, other);
    }

    protected virtual IMergeResult MergeObject(JObject update, JObject other, IJsonMergeContext context)
    {
        IEnumerable<MergeResult> diffs = from key in UnionKeys(update, other)
            let diff = (MergeResult) Merge(update[key], other[key], context.Next(key))
            select diff;
            
        return context.Multiple(diffs, update, other);
    }

    protected virtual IMergeResult MergeArray(JArray update, JArray other, IJsonMergeContext context)
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