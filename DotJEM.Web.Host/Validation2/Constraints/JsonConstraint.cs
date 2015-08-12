using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public abstract class JsonConstraint
    {
        public abstract JsonConstraintResult Matches(IJsonValidationContext context, JToken token);

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

    }
}