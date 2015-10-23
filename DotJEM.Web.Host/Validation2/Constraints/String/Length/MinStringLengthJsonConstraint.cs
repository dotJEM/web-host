using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String.Length
{
    [JsonConstraintDescription("length must be more than or equal to '{minLength}'.")]
    public class MinStringLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int minLength;

        public MinStringLengthJsonConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length >= minLength;
        }
    }
}