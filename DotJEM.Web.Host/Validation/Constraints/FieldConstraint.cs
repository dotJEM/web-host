using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public abstract class FieldConstraint : IFieldConstraint
    {
        public void Validate(JToken token, IValidationCollector collector)
        {
            if (token != null)
            {
                OnValidate(token, collector);
            }
        }

        protected abstract void OnValidate(JToken token, IValidationCollector context);
    }
}