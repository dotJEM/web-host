using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class ExactLengthFieldConstraint : FieldConstraint
    {
        private readonly int length;

        public ExactLengthFieldConstraint(int length)
        {
            this.length = length;
        }

        protected override void OnValidate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (Matches(token))
                return;

            collector.AddError("Length must be '{0}'.", length);
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return value.Length == length;
        }
    }
}