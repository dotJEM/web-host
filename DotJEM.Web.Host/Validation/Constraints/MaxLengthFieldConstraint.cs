using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class MaxLengthFieldConstraint : FieldConstraint
    {
        private readonly int maxLength;

        public MaxLengthFieldConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            if (Matches(token)) 
                return;

            context.AddError("Length must be less than '{0}'.", maxLength);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return value.Length <= maxLength;
        }
    }
}