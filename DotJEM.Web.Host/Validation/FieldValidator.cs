using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public class FieldValidator : IFieldValidator
    {
        private readonly JPath field;
        private readonly IFieldConstraint constraint;

        public FieldValidator(JPath field, IFieldConstraint constraint)
        {
            this.field = field;
            this.constraint = constraint;
        }

        public FieldValidationResults Validate(JObject entity)
        {
            //TODO: Account for propertyNames with "."...
            JToken token = entity.SelectToken(field.ToString());

            ValidationCollector collector = new ValidationCollector();
            constraint.Validate(token, collector);
            return new FieldValidationResults(field, token, collector);
        }
    }
}