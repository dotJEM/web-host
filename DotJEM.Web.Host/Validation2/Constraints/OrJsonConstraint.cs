using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public sealed class OrJsonConstraint : CompositeJsonConstraint
    {
        public OrJsonConstraint()
        {
        }

        public OrJsonConstraint(params JsonConstraint[] constraints)
            : base(constraints)
        {
        }

        public override JsonConstraint Optimize()
        {
            return OptimizeAs<OrJsonConstraint>();
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            //TODO: Aggregated JsonConstraintResult!
            return Constraints.Any(c => c.Matches(context, token).Value);
        }

        public override string ToString()
        {
            return "( " + string.Join(" OR ", Constraints) + " )";
        }
    }
}