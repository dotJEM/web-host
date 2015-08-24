using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public abstract class CompositeJsonConstraintResult : JsonConstraintResult
    {
        protected List<JsonConstraintResult> Results { get; private set; }

        public override string Message
        {
            get
            {
                return Results.Aggregate(new StringBuilder(), (builder, result) =>
                {
                    if (!result.Value)
                    {
                        builder.AppendLine(result.Message);
                    }
                    return builder;
                }).ToString();
            }
        }
        
        protected CompositeJsonConstraintResult(List<JsonConstraintResult> results)
        {
            Results = results;
        }

        protected TResult OptimizeAs<TResult>() where TResult : CompositeJsonConstraintResult, new()
        {
            return Results
                .Select(c => c.Optimize())
                .Aggregate(new TResult(), (c, next) =>
                {
                    TResult and = next as TResult;
                    if (and != null)
                    {
                        c.Results.AddRange(and.Results);
                    }
                    else
                    {
                        c.Results.Add(next);
                    }
                    return c;
                });
        }
    }
}