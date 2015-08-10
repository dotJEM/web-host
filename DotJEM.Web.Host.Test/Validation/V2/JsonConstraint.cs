using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonConstraint
    {
        public abstract JsonConstraintResult Matches(IValidationContext context, JToken token);

        public static JsonConstraint operator &(JsonConstraint x, JsonConstraint y)
        {
            return new AndJsonConstraint(x, y);
        }

        public static JsonConstraint operator |(JsonConstraint x, JsonConstraint y)
        {
            return new OrJsonConstraint(x, y);
        }

        public static JsonConstraint operator !(JsonConstraint x)
        {
            return new NotJsonConstraint(x);
        }

        public virtual JsonConstraint Optimize()
        {
            return this;
        }

        protected JsonConstraintResult True()
        {
            return new BasicJsonConstraintResult(true, null, GetType());
        }

        protected JsonConstraintResult False(string message = null)
        {
            return new BasicJsonConstraintResult(false, message, GetType());
        }
    }

    public abstract class CompositeJsonConstraint : JsonConstraint
    {
        public List<JsonConstraint> Constraints { get; private set; }

        protected CompositeJsonConstraint(params JsonConstraint[] constraints)
        {
            Constraints = constraints.ToList();
        }

        protected TConstraint OptimizeAs<TConstraint>() where TConstraint : CompositeJsonConstraint, new()
        {
            return Constraints
                .Select(c => c.Optimize())
                .Aggregate(new TConstraint(), (c, next) =>
                {
                    TConstraint and = next as TConstraint;
                    if (and != null)
                    {
                        c.Constraints.AddRange(and.Constraints);
                    }
                    else
                    {
                        c.Constraints.Add(next);
                    }
                    return c;
                });
        }
    }

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

        public override JsonConstraintResult Matches(IValidationContext context, JToken token)
        {
            return Constraints.All(c => c.Matches(context, token).Value);
        }

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints) + " )";
        }
    }

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

        public override JsonConstraintResult Matches(IValidationContext context, JToken token)
        {
            //TODO: Aggregated JsonConstraintResult!
            return Constraints.Any(c => c.Matches(context, token).Value);
        }

        public override string ToString()
        {
            return "( " + string.Join(" OR ", Constraints) + " )";
        }
    }

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
        public override JsonConstraintResult Matches(IValidationContext context, JToken token)
        {
            //TODO: Aggregated JsonConstraintResult!
            return !Constraint.Matches(context, token);
        }

        public override string ToString()
        {
            return "!" + Constraint;
        }
    }

    public class NamedJsonConstraint : JsonConstraint
    {
        public string Name { get; private set; }

        public NamedJsonConstraint(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override JsonConstraintResult Matches(IValidationContext context, JToken token)
        {
            return True();
        }
    }
}