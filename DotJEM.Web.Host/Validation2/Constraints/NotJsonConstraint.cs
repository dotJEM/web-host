using System;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public sealed class NotJsonConstraint : JsonConstraint
    {
        public JsonConstraint Constraint { get; private set; }

        public NotJsonConstraint(JsonConstraint constraint)
        {
            Constraint = constraint;
        }

        public override JsonConstraint Optimize()
        {
            NotJsonConstraint not = Constraint as NotJsonConstraint;
            return not != null ? not.Constraint : base.Optimize();
        }

        internal override JsonConstraintResult DoMatch(IJsonValidationContext context, JToken token)
        {
            return !Constraint.Matches(context, token);
        }

        public override bool Matches(IJsonValidationContext context, JToken token)
        {
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            return "!" + Constraint;
        }
    }
}