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

        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            string value = (string)token;
            if (value.Length == length)
                return;

            context.AddError("Length must be '{0}'.", length);
        }
    }
}