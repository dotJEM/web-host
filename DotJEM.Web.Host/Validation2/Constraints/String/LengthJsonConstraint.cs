using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String length must be from '{minLength}' to '{maxLength}'.")]
    public class LengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int minLength;
        private readonly int maxLength;

        public LengthJsonConstraint(int minLength, int maxLength)
        {
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length >= minLength && value.Length <= maxLength;
        }
    }
}