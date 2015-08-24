using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String length must be less than or equal to '{maxLength}'.")]
    public class MaxLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int maxLength;

        public MaxLengthJsonConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length <= maxLength;
        }
    }
}