using System;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("be equal to '{value}' ({comparison}).")]
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