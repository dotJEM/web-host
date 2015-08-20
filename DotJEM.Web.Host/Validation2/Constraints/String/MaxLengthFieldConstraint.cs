using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Results;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    public class MaxLengthFieldConstraint : JsonConstraint
    {
        private readonly int maxLength;

        public MaxLengthFieldConstraint(int maxLength)
        {
            this.maxLength = maxLength;
            //Describe("Length must be less than '{0}'.", maxLength);
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            return value.Length <= maxLength;
        }
    }
}