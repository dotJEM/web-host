namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class NotJsonConstraintResult : JsonConstraintResult
    {
        public JsonConstraintResult Result { get; private set; }

        public NotJsonConstraintResult(JsonConstraintResult result)
            : base(!result.Value)
        {
            Result = result;
        }

        public override JsonConstraintResult Optimize()
        {
            NotJsonConstraintResult not = Result as NotJsonConstraintResult;
            return not != null ? not.Result : base.Optimize();
        }
    }
}