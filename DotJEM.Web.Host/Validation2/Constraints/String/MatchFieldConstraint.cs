using System;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String must match the expression: '{expression}'.")]
    public class MatchJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly Regex expression;

        public MatchJsonConstraint(string expression, RegexOptions options = RegexOptions.Compiled)
        {
            this.expression = new Regex(expression, options);
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return expression.IsMatch(value);
        }
    }

    [JsonConstraintDescription("String must be equal to '{value}' ({comparison}).")]
    public class StringEqualsJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly string value;
        private readonly StringComparison comparison;
        
        public StringEqualsJsonConstraint(string value, StringComparison comparison = StringComparison.Ordinal)
        {
            this.value = value;
            this.comparison = comparison;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Equals(this.value, comparison);
        }
    }
}