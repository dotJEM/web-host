using System;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    [Obsolete]
    public class MatchFieldConstraint : FieldConstraint
    {
        private readonly Regex expression;

        public MatchFieldConstraint(string expression, RegexOptions options = RegexOptions.Compiled)
        {
            this.expression = new Regex(expression, options);
        }

        protected override void OnValidate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (Matches(token))
                return;

            collector.AddError("Value must match '{0}'.", expression);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return expression.IsMatch(value);
        }
    }

    [Obsolete]
    public class StringEqualToFieldConstraint : FieldConstraint
    {
        private readonly string value;
        private readonly StringComparison comparison;

        public StringEqualToFieldConstraint(string value, StringComparison comparison)
        {
            this.value = value;
            this.comparison = comparison;
        }

        protected override void OnValidate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (Matches(value))
                return;

            //TODO: Add comparison information so we don't get : value foo must be equal FOO when ignore case.
            collector.AddError("Value must be equal to '{0}'.", value);
        }

        protected override bool OnMatches(JToken token)
        {
            string val = (string)token;
            return val.Equals(this.value, comparison);
        }
    }

}