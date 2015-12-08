using System;

namespace DotJEM.Web.Host.Providers.Services.DiffMerge
{
    public class JsonMergeConflictException : Exception
    {
        public MergeResult MergeResult { get; }

        public JsonMergeConflictException(MergeResult result)
            : base(result.ToString())
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
}