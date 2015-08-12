namespace DotJEM.Web.Host.Validation2.Rules.Results
{
    public sealed class NotJsonRuleResult : JsonRuleResult
    {
        public JsonRuleResult Result { get; private set; }

        public override bool Value
        {
            get { return !Result.Value; }
        }

        public NotJsonRuleResult(JsonRuleResult result)
        {
            Result = result;
        }

        public override JsonRuleResult Optimize()
        {
            NotJsonRuleResult not = Result as NotJsonRuleResult;
            return not != null ? not.Result : base.Optimize();
        }
    }
}