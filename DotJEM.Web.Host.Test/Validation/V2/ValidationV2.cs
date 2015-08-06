using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    [TestFixture]
    public class ValidationV2ConstraintBuilder
    {
        [Test]
        public void Validate_InvalidData_ShouldReturnErrors()
        {
            var constraint = (N & N) | (N & N & !N & N);

            string str = constraint.ToString();

            Assert.That(constraint, Is.TypeOf<OrJsonFieldConstraint>());

            constraint = constraint.Optimize();

            string str2 = constraint.ToString();
            Assert.That(constraint, Is.TypeOf<OrJsonFieldConstraint>());
        }

        public int counter = 1;

        public Named N
        {
            get { return new Named("" + counter++); }
        }
    }

    public interface IGuardFactory { }
    public interface IValidatorFactory { }

    public static class JsonValidatorExtensions
    {
        public static dynamic Defined(this IGuardFactory self)
        {
            return null;
        }

        public static dynamic BeDefined(this IValidatorFactory self)
        {
            return null;
        }

        public static dynamic Not(this IValidatorFactory self)
        {
            return null;
        }

        public static dynamic BeEqual(this IValidatorFactory self, object value)
        {
            return null;
        }
    }

    public class JsonValidator
    {
        protected IGuardFactory Is { get; set; }
        protected IValidatorFactory Must { get; set; }
        protected IValidatorFactory Should { get; set; }

        protected dynamic When(dynamic guard)
        {
            return null;
        }

        protected dynamic When(string selector, dynamic guard)
        {
            return null;
        }

        protected dynamic Field(string selector, dynamic guard)
        {
            return null;
        }
    }

    public class SpecificValidator : JsonValidator
    {
        public SpecificValidator()
        {
            When("A", Is.Defined()).Validate("A", Must.BeDefined());
            When(Field("A", Is.Defined()) | Field("B", Is.Defined()))
                .Validate(
                      Field("A", Must.BeEqual("") | Must.BeEqual(""))
                    & Field("B", Must.Not().Equals("")));
        }

    }

    public class JsonValidationRule
    {
        
    }

    public class JsonValidationResult
    {
        public static JsonValidationResult operator &(JsonValidationResult x, JsonValidationResult y)
        {
            return new JsonValidationResult();
        }

        public static JsonValidationResult operator |(JsonValidationResult x, JsonValidationResult y)
        {
            return new JsonValidationResult();
        }

        public static JsonValidationResult operator !(JsonValidationResult x)
        {
            return new JsonValidationResult();
        }
    }

    public abstract class JsonFieldConstraint
    {
        public virtual JsonValidationResult Validate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return OnValidate(token, context, collector);
        }

        protected abstract JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector);

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

        protected JsonValidationResult None()
        {
            return new JsonValidationResult();
        }

        protected JsonValidationResult Error(string format, params object[] args)
        {
            return new JsonValidationResult();
        }

        public virtual JsonFieldConstraint Optimize()
        {
            return this;
        }
    }

    public abstract class AllowNullFieldConstraint : JsonFieldConstraint
    {
        public override JsonValidationResult Validate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            if (token != null)
            {
                return base.Validate(token, context, collector);
            }
            return None();
        }
    }

    public abstract class CompositeJsonFieldConstraint : JsonFieldConstraint
    {
        public List<JsonFieldConstraint> Constraints { get; private set; }

        protected CompositeJsonFieldConstraint(params JsonFieldConstraint[] constraints)
        {
            Constraints = constraints.ToList();
        }
    }

    public sealed class AndJsonFieldConstraint : CompositeJsonFieldConstraint
    {
        public AndJsonFieldConstraint(params JsonFieldConstraint[] constraints)
            : base(constraints)
        {
        }

        protected override JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return Constraints
                .Aggregate(new JsonValidationResult(),
                    (prev, next) => prev & next.Validate(token, context, collector));
        }

        public override JsonFieldConstraint Optimize()
        {
            return Constraints
                .Select(c => c.Optimize())
                .Aggregate(new AndJsonFieldConstraint(), (c, next) =>
                {
                    AndJsonFieldConstraint and = next as AndJsonFieldConstraint;
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

        public override string ToString()
        {
            return "( " + string.Join(" AND ", Constraints) + " )";
        }
    }

    public sealed class OrJsonFieldConstraint : CompositeJsonFieldConstraint
    {
        public OrJsonFieldConstraint(params JsonFieldConstraint[] constraints)
            : base(constraints)
        {
        }

        protected override JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return Constraints
                .Aggregate(new JsonValidationResult(),
                    (prev, next) => prev | next.Validate(token, context, collector));
        }

        public override JsonFieldConstraint Optimize()
        {
            return Constraints
                .Select(c => c.Optimize())
                .Aggregate(new OrJsonFieldConstraint(), (c, next) =>
                {
                    OrJsonFieldConstraint or = next as OrJsonFieldConstraint;
                    if (or != null)
                    {
                        c.Constraints.AddRange(or.Constraints);
                    }
                    else
                    {
                        c.Constraints.Add(next);
                    }
                    return c;
                });
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

        protected override JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return !Constraint.Validate(token, context, collector);
        }

        public override JsonFieldConstraint Optimize()
        {
            NotJsonFieldConstraint not = Constraint as NotJsonFieldConstraint;
            return not != null ? not.Constraint : base.Optimize();
        }

        public override string ToString()
        {
            return "!" + Constraint;
        }
    }

    public class Named : JsonFieldConstraint
    {
        public string Name { get; private set; }

        public Named(string name)
        {
            this.Name = name;
        }

        protected override JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return None();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class LengthJsonFieldConstraint : JsonFieldConstraint
    {
        private readonly int minLength;
        private readonly int maxLength;

        public LengthJsonFieldConstraint(int minLength, int maxLength)
        {
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        protected void OnValidate(IValidationContext context1, JToken token, IValidationCollector collector)
        {
            string value = (string)token;
            if (value.Length >= minLength && value.Length <= maxLength)
                return;

            collector.AddError("Length must be less than '{0}'.", minLength);
        }

        protected override JsonValidationResult OnValidate(JToken token, IValidationContext context, IValidationCollector collector)
        {
            return None();
        }
    }
}
