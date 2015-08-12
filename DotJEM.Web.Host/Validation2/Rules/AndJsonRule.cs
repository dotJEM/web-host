using System.Linq;
using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Rules
{
    public sealed class AndJsonRule : CompositeJsonRule
    {
        public AndJsonRule()
        {
        }

        public AndJsonRule(params JsonRule[] rules) 
            : base(rules)
        {
        }

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            //TODO: Lazy
            return Rules.Aggregate(new AndJsonRuleResult(), (result, rule) => result & rule.Test(context, entity));
        }

        public override JsonRule Optimize()
        {
            return OptimizeAs<AndJsonRule>();
        }
    }
}