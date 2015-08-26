using System;
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

        internal override JsonConstraintResult DoMatch(IJsonValidationContext context, JToken token)
        {
            return Constraints.Aggregate((JsonConstraintResult) null, (a, b) => a & b.Matches(context, token));
        }

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints) + " )";
        }
    }
}