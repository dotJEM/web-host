using System;
using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    [JsonConstraintDescription("{Described}")]
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
            return Constraints.Aggregate((JsonConstraintResult) null, (a, b) => a & b.DoMatch(context, token));
        }

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints.Select(c => c.Describe())) + " )";
        }
        private string Described => ToString();
    }
}