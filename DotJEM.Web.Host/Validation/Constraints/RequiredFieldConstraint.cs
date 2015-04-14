using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class RequiredFieldConstraint : IFieldConstraint
    {
        public void Validate(JToken token, IValidationCollector context)
        {
            if (token != null)
                return;

            context.AddError("Value is required.");
        }
    }
}