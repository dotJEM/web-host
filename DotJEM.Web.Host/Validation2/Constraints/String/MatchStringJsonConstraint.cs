using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.String
{
    [JsonConstraintDescription("String must match the expression: '{expression}'.")]
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