using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String length must be more than or equal to '{maxLength}'.")]
    public class MinLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int minLength;

        public MinLengthJsonConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length >= minLength;
        }
    }
}