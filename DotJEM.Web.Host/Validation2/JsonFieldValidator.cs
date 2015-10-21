using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
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
            JsonRuleResult gr = guard.Test(context, entity);
            return !gr.Value 
                ? null 
                : rule.Test(context, entity);
        }

        public JsonFieldValidatorDescription Describe()
        {
            return new JsonFieldValidatorDescription(guard.Describe(), rule.Describe());
        }
    }
}