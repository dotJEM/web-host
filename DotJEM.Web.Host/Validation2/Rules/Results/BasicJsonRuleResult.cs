using DotJEM.Web.Host.Validation2.Constraints.Results;

namespace DotJEM.Web.Host.Validation2.Rules.Results
{
    public sealed class BasicJsonRuleResult : JsonRuleResult
    {
        private readonly JsonConstraintResult result;

        public override bool Value { get { return result.Value; } }
        public string Path { get; private set; }

        public BasicJsonRuleResult(string path, JsonConstraintResult result)
        {
            Path = path;
            this.result = result.Optimize();
        }
    }
}