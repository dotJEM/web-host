using System;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    [Obsolete]
    public class LengthFieldConstraint : FieldConstraint
    {
        private readonly int minLength;
        private readonly int maxLength;

        public LengthFieldConstraint(int minLength, int maxLength)
        {
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        protected override void OnValidate(IValidationContext context1, JToken token, IValidationCollector collector)
        {
            if (Matches(token))
                return;

            collector.AddError("Length must be less than '{0}'.", minLength);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return value.Length >= minLength && value.Length <= maxLength;
        }
    }
}