using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class AndJsonConstraintResult : CompositeJsonConstraintResult
    {
        public override bool Value
        {
            get { return Results.All(r => r.Value); }
        }

        public AndJsonConstraintResult() 
            : base(new List<JsonConstraintResult>())
        {
        }

        public AndJsonConstraintResult(params JsonConstraintResult[] results)
            : base(results.ToList())
        {
        }

        public AndJsonConstraintResult(List<JsonConstraintResult> results) 
            : base(results)
        {
        }

        public override JsonConstraintResult Optimize()
        {
            return OptimizeAs<AndJsonConstraintResult>();
        }
    }
}