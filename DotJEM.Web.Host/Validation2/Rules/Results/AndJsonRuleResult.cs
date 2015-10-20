using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Validation2.Rules.Results
{
    public sealed class AndJsonRuleResult : CompositeJsonRuleResult
    {
        public override bool Value => Results.All(r => r.Value);

        public AndJsonRuleResult() 
            : base(new List<JsonRuleResult>())
        {
        }

        public AndJsonRuleResult(params JsonRuleResult[] results)
            : base(results.ToList())
        {
        }

        public AndJsonRuleResult(List<JsonRuleResult> results) 
            : base(results)
        {
        }

        public override JsonRuleResult Optimize()
        {
            return OptimizeAs<AndJsonRuleResult>();
        }
    }
}