using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class OrJsonConstraintResult : CompositeJsonConstraintResult
    {
        public override bool Value
        {
            get { return Results.Any(r => r.Value); }
        }

        public OrJsonConstraintResult() 
            : base(new List<JsonConstraintResult>())
        {
        }

        public OrJsonConstraintResult(params JsonConstraintResult[] results)
            : base(results.ToList())
        {
        }

        public OrJsonConstraintResult(List<JsonConstraintResult> results) 
            : base(results)
        {
        }

        public override JsonConstraintResult Optimize()
        {
            return OptimizeAs<OrJsonConstraintResult>();
        }
    }
}