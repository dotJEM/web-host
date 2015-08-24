namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public abstract class JsonConstraintResult
    {
        public abstract bool Value { get; }
        public abstract string Message { get; }

        public virtual JsonConstraintResult Optimize()
        {
            return this;
        }
        
        public static implicit operator JsonConstraintResult(bool value)
        {
            return new BasicJsonConstraintResult(value, null, null);
        }

        public static JsonConstraintResult operator &(JsonConstraintResult x, JsonConstraintResult y)
        {
            return new AndJsonConstraintResult(x, y);
        }

        public static JsonConstraintResult operator |(JsonConstraintResult x, JsonConstraintResult y)
        {
            return new OrJsonConstraintResult(x, y);
        }

        public static JsonConstraintResult operator !(JsonConstraintResult x)
        {
            return new NotJsonConstraintResult(x);
        }

    }
}
