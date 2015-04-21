using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class CompositeFieldConstraint : IFieldConstraint
    {
        private readonly IFieldConstraint left;
        private readonly IFieldConstraint right;

        public CompositeFieldConstraint(IFieldConstraint left, IFieldConstraint right)
        {
            this.left = left;
            this.right = right;
        }

        public void Validate(JToken token, IValidationCollector context)
        {
            left.Validate(token, context);
            right.Validate(token, context);
        }

        public bool Matches(JToken token)
        {
            //TODO: Support OR?
            return left.Matches(token) && right.Matches(token);
        }
    }
}