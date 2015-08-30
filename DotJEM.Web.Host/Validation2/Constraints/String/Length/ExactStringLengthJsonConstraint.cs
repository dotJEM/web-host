using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String.Length
{
    [JsonConstraintDescription("String length must be '{length}'.")]
    public class ExactStringLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int length;

        public ExactStringLengthJsonConstraint(int length)
        {
            this.length = length;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length == length;
        }
    }
}