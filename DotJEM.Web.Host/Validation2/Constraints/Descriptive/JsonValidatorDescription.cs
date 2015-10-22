using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotJEM.Web.Host.Validation2.Constraints.Descriptive
{
    public class JsonValidatorDescription
    {
        private readonly JsonValidator validator;
        private readonly List<JsonFieldValidatorDescription> descriptions;

        public JsonValidatorDescription(JsonValidator validator, List<JsonFieldValidatorDescription> descriptions)
        {
            this.validator = validator;
            this.descriptions = descriptions;
        }

        public override string ToString()
        {
            StringWriter writer = new StringWriter();

            descriptions.ForEach(d => writer.WriteLine(d.ToString()));

            return writer.ToString();
        }
    }

    public class JsonFieldValidatorDescription
    {
        private readonly JsonRuleDescription rule;
        private readonly JsonRuleDescription guard;

        public JsonFieldValidatorDescription(JsonRuleDescription guard, JsonRuleDescription rule)
        {
            this.rule = rule;
            this.guard = guard;
        }


        public override string ToString()
        {
            return $"When {guard} then {rule}";
        }
    }

    public class JsonRuleDescription
    {
        private readonly string alias;
        private readonly string selector;
        private readonly JsonConstraint constraint;

        public JsonRuleDescription(string alias, string selector, JsonConstraint constraint)
        {
            this.alias = alias;
            this.selector = selector;
            this.constraint = constraint;
        }

        public override string ToString()
        {
            return $"{alias} should {constraint.Describe()}";
        }
    }
}