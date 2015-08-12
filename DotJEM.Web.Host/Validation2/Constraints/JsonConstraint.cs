using System;
using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public abstract class JsonConstraint
    {
        public JsonConstraintDescription Description { get; private set; }

        protected JsonConstraint()
        {
            Description = new JsonConstraintDescription(GetType(), "Constraint failed");
        }

        public abstract JsonConstraintResult Matches(IJsonValidationContext context, JToken token);

        protected JsonConstraintResult True()
        {
            return new BasicJsonConstraintResult(true, null, GetType());
        }

        protected JsonConstraintResult True(string format, params object[] args)
        {
            return new BasicJsonConstraintResult(false, string.Format(format, args), GetType());
        }

        protected JsonConstraintResult False()
        {
            return new BasicJsonConstraintResult(false, null, GetType());
        }

        protected JsonConstraintResult False(string format, params object[] args)
        {
            return new BasicJsonConstraintResult(false, string.Format(format, args), GetType());
        }

        protected void Describe(string format, params object[] args)
        {
            this.Description = new JsonConstraintDescription(GetType(), format, args);
        }

        public virtual JsonConstraint Optimize()
        {
            return this;
        }


        #region Operator Overloads
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
        #endregion
    }

    public class JsonConstraintDescription
    {
        private readonly Type type;
        private readonly string errorFormat;
        private readonly object[] args;

        public JsonConstraintDescription(Type type, string errorFormat, params object[] args)
        {
            this.type = type;
            this.errorFormat = errorFormat;
            this.args = args;
        }

        public override string ToString()
        {
            //TODO: Use type in any way?
            return args.Any() ? string.Format(errorFormat, args) : errorFormat;
        }
    }
}