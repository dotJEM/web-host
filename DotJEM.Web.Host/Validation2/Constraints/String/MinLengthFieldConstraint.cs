using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    public class MinLengthFieldConstraint : FieldConstraint
    {
        private readonly int minLength;

        public MinLengthFieldConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        protected override void OnValidate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (Matches(token))
                return;

            collector.AddError("Length must be greater than '{0}'.", minLength);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return value.Length >= minLength;
        }
    }
}