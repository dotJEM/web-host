using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2
{
    public class JsonFieldValidator
    {
        private readonly JsonRule guard;
        private readonly JsonRule rule;

        public JsonFieldValidator(JsonRule guard, JsonRule rule)
        {
            this.guard = guard.Optimize();
            this.rule = rule.Optimize();
        }

        public JsonRuleResult Validate(IJsonValidationContext context, JObject entity)
        {
            var gr = guard.Test(context, entity);
            if (!gr.Value)
                return null;

            return rule.Test(context, entity);
        }
    }
}