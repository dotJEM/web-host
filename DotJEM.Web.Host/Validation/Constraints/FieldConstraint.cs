using System;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    [Obsolete]
    public abstract class FieldConstraint : IFieldConstraint
    {
        public void Validate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            if (token != null && token.Type != JTokenType.Null)
            {
                OnValidate(context, token, collector);
            }
        }

        public bool Matches(JToken token)
        {
            return token != null && token.Type != JTokenType.Null && OnMatches(token);
        }

        protected abstract void OnValidate(IValidationContext context, JToken token, IValidationCollector collector);
        protected abstract bool OnMatches(JToken token);

    }
}