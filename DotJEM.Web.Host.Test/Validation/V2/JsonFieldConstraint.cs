using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonFieldConstraint
    {
        public abstract bool Matches(IValidationContext context, JToken token);

        public static JsonFieldConstraint operator &(JsonFieldConstraint x, JsonFieldConstraint y)
        {
            return new AndJsonFieldConstraint(x, y);
        }

        public static JsonFieldConstraint operator |(JsonFieldConstraint x, JsonFieldConstraint y)
        {
            return new OrJsonFieldConstraint(x, y);
        }

        public static JsonFieldConstraint operator !(JsonFieldConstraint x)
        {
            return new NotJsonFieldConstraint(x);
        }

        public virtual JsonFieldConstraint Optimize()
        {
            return this;
        }
    }
    public abstract class CompositeJsonFieldConstraint : JsonFieldConstraint
    {
        public List<JsonFieldConstraint> Constraints { get; private set; }

        protected CompositeJsonFieldConstraint(params JsonFieldConstraint[] constraints)
        {
            Constraints = constraints.ToList();
        }

        protected TConstraint OptimizeAs<TConstraint>() where TConstraint : CompositeJsonFieldConstraint, new()
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

    public sealed class AndJsonFieldConstraint : CompositeJsonFieldConstraint
    {
        public AndJsonFieldConstraint()
        {
        }

        public AndJsonFieldConstraint(params JsonFieldConstraint[] constraints)
            : base(constraints)
        {
        }
        
        public override JsonFieldConstraint Optimize()
        {
            return OptimizeAs<AndJsonFieldConstraint>();
        }

        public override bool Matches(IValidationContext context, JToken token)
        {
            return Constraints.All(c => c.Matches(context, token));
        }

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints) + " )";
        }
    }

    public sealed class OrJsonFieldConstraint : CompositeJsonFieldConstraint
    {
        public OrJsonFieldConstraint()
        {
        }

        public OrJsonFieldConstraint(params JsonFieldConstraint[] constraints)
            : base(constraints)
        {
        }

        public override JsonFieldConstraint Optimize()
        {
            return OptimizeAs<OrJsonFieldConstraint>();
        }

        public override bool Matches(IValidationContext context, JToken token)
        {
            return Constraints.Any(c => c.Matches(context, token));
        }

        public override string ToString()
        {
            return "( " + string.Join(" OR ", Constraints) + " )";
        }
    }

    public sealed class NotJsonFieldConstraint : JsonFieldConstraint
    {
        public JsonFieldConstraint Constraint { get; private set; }

        public NotJsonFieldConstraint(JsonFieldConstraint constraint)
        {
            Constraint = constraint;
        }

        public override JsonFieldConstraint Optimize()
        {
            NotJsonFieldConstraint not = Constraint as NotJsonFieldConstraint;
            return not != null ? not.Constraint : base.Optimize();
        }
        public override bool Matches(IValidationContext context, JToken token)
        {
            return !Constraint.Matches(context, token);
        }

        public override string ToString()
        {
            return "!" + Constraint;
        }
    }

    public class NamedJsonFieldConstraint : JsonFieldConstraint
    {
        public string Name { get; private set; }

        public NamedJsonFieldConstraint(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Matches(IValidationContext context, JToken token)
        {
            return true;
        }
    }
}