using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge;

public class JsonMergeConflictException : Exception
{
    public IMergeResult MergeResult { get; }

    public JsonMergeConflictException(IMergeResult result)
        : base(result.ToString())
    {
        MergeResult = new ConflictedMergeResult(result.Update, result.Other, result.Origin, result.Conflicts, result.HasConflicts);
    }

    public JsonMergeConflictException(IMergeResult result, string message)
        : base(message)
    {
        MergeResult = new ConflictedMergeResult(result.Update, result.Other, result.Origin, result.Conflicts, result.HasConflicts);
    }

    public JsonMergeConflictException(IMergeResult result, string message, Exception inner)
        : base(message, inner)
    {
        MergeResult = new ConflictedMergeResult(result.Update, result.Other, result.Origin, result.Conflicts, result.HasConflicts);
    }
}
public class ConflictedMergeResult : IMergeResult
{

    public JToken Update { get; }
    public JToken Merged => null;
    public JToken Other { get; }
    public JToken Origin { get; }
    public JObject Conflicts { get; }
    public bool HasConflicts { get; }

    public ConflictedMergeResult(JToken update, JToken other, JToken origin, JObject conflicts, bool hasConflicts)
    {
        Update = update;
        Other = other;
        Origin = origin;
        Conflicts = conflicts;
        HasConflicts = hasConflicts;
    }

}