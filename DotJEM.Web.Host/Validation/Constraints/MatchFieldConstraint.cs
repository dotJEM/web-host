using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class MatchFieldConstraint : FieldConstraint
    {
        private readonly Regex expression;

        public MatchFieldConstraint(string expression, RegexOptions options = RegexOptions.Compiled)
        {
            this.expression = new Regex(expression, options);
        }

        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            string value = (string)token;
            if (expression.IsMatch(value))
                return;

            context.AddError("Value must match '{0}'.", expression);
        }
    }
}