namespace DotJEM.Web.Host.Validation2.Constraints.Results
{
    public sealed class NotJsonConstraintResult : JsonConstraintResult
    {
        public JsonConstraintResult Result { get; private set; }

        public override bool Value
        {
            get { return !Result.Value; }
        }

        public override string Message
        {
            get { return Result.Message; }
        }

        public NotJsonConstraintResult(JsonConstraintResult result)
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