using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Rules
{
    public sealed class NotJsonRule : JsonRule
    {
        public JsonRule Rule { get; private set; }

        public NotJsonRule(JsonRule rule)
        {
            Rule = rule;
        }

        public override JsonRuleResult Test(IJsonValidationContext contenxt, JObject entity)
        {
            return !Rule.Test(contenxt, entity);
        }

        public override JsonRule Optimize()
        {
            NotJsonRule not = Rule as NotJsonRule;
            return not != null ? not.Rule : base.Optimize();
        }
    }
}