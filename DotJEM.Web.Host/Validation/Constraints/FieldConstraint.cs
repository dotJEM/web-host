using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public abstract class FieldConstraint : IFieldConstraint
    {
        public void Validate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (token != null)
            {
                OnValidate(context, token, collector);
            }
        }

        public bool Matches(JToken token)
        {
            return token != null && OnMatches(token);
        }

        protected abstract void OnValidate(IValidationContext context, JToken token, IValidationCollector collector);
        protected abstract bool OnMatches(JToken token);

    }
}