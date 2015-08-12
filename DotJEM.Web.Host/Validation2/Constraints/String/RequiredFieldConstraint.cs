using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    public class RequiredFieldConstraint : IFieldConstraint
    {
        public void Validate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (Matches(token))
                return;

            collector.AddError("Value is required.");
        }

        public bool Matches(JToken token)
        {
            return token != null;
        }
    }
}