using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class MinLengthFieldConstraint : FieldConstraint
    {
        private readonly int minLength;

        public MinLengthFieldConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            if (Matches(token))
                return;

            context.AddError("Length must be greater than '{0}'.", minLength);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return value.Length >= minLength;
        }
    }
}