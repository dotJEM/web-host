using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Rules
{
    public sealed class BasicJsonRule : JsonRule
    {
        private static readonly Regex arraySelector = new Regex(@".*\[\*|.+].*", RegexOptions.Compiled);

        private readonly string selector;
        private readonly JsonConstraint constraint;
        private readonly bool hasArray;

        public BasicJsonRule(string selector, JsonConstraint constraint)
        {
            this.selector = selector;
            this.constraint = constraint.Optimize();
            this.hasArray = arraySelector.IsMatch(selector);
        }

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            return new AndJsonRuleResult(
                (from token in SelectTokens(entity)
                 select (JsonRuleResult)new BasicJsonRuleResult(token?.Path ?? selector, constraint.DoMatch(context, token))).ToList());
        }

        private IEnumerable<JToken> SelectTokens(JObject entity)
        {
            if (hasArray)
            {
                return entity.SelectTokens(selector).ToList();
            }
            return new[] { entity.SelectToken(selector) };
        }
    }
}