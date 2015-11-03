using DotJEM.Web.Host.Validation2.Constraints.Results;

namespace DotJEM.Web.Host.Validation2.Rules.Results
{
    public sealed class BasicJsonRuleResult : JsonRuleResult
    {
        private readonly JsonConstraintResult result;

        public override bool Value => result.Value;
        public string Selector { get; }
        public string Path { get; }

        public BasicJsonRuleResult(string selector, string path, JsonConstraintResult result)
        {
            Path = path;
            Selector = selector;

            this.result = result.Optimize();
        }
    }

    public sealed class AnyJsonRuleResult : JsonRuleResult
    {
        public override bool Value { get; } = true;
    }
}