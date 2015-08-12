using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public sealed class AndJsonConstraint : CompositeJsonConstraint
    {
        public AndJsonConstraint()
        {
        }

        public AndJsonConstraint(params JsonConstraint[] constraints)
            : base(constraints)
        {
        }
        
        public override JsonConstraint Optimize()
        {
            return OptimizeAs<AndJsonConstraint>();
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            return Constraints.All(c => c.Matches(context, token).Value);
        }

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints) + " )";
        }
    }
}