using System;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    [Obsolete]
    public class CompositeFieldConstraint : IFieldConstraint
    {
        private readonly IFieldConstraint left;
        private readonly IFieldConstraint right;

        public CompositeFieldConstraint(IFieldConstraint left, IFieldConstraint right)
        {
            this.left = left;
            this.right = right;
        }

        public void Validate(IValidationContext context, JToken token, IValidationCollector collector)
        {
            left.Validate(context, token, collector);
            right.Validate(context, token, collector);
        }

        public bool Matches(JToken token)
        {
            //TODO: Support OR?
            return left.Matches(token) && right.Matches(token);
        }
    }
}