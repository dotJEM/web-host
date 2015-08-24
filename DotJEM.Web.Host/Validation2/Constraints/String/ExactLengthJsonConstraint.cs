using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String length must be '{length}'.")]
    public class ExactLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int length;

        public ExactLengthJsonConstraint(int length)
        {
            this.length = length;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length == length;
        }
    }
}