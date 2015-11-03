using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("match the expression: '{expression}'.")]
    public class MatchStringJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly Regex expression;

        public MatchStringJsonConstraint(Regex expression)
        {
            this.expression = expression;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return expression.IsMatch(value);
        }
    }
}