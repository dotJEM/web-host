using System.Linq;
using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Rules
{
    public sealed class OrJsonRule : CompositeJsonRule
    {
        public OrJsonRule()
        {
        }

        public OrJsonRule(params JsonRule[] rules)
            : base(rules)
        {
        }

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            //TODO: Lazy
            return Rules.Aggregate(new OrJsonRuleResult(), (result, rule) => result | rule.Test(context, entity));
        }

        public override JsonRule Optimize()
        {
            return OptimizeAs<OrJsonRule>();
        }
    }
}