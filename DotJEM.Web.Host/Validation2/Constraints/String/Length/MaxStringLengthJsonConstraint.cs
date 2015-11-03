using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String.Length
{
    [JsonConstraintDescription("length must be less than or equal to '{maxLength}'.")]
    public class MaxStringLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int maxLength;

        public MaxStringLengthJsonConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length <= maxLength;
        }
    }
}