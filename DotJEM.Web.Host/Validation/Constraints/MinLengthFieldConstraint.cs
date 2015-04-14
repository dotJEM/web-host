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
            string value = (string)token;
            if (value.Length >= minLength)
                return;

            context.AddError("Length must be greater than '{0}'.", minLength);
        }
    }
}