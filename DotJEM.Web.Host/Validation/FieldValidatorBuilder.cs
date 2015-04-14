using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Constraints;

namespace DotJEM.Web.Host.Validation
{
    public class FieldValidatorBuilder : IFieldValidatorBuilder
    {
        private IFieldConstraint constraint = new NullFieldConstraint();

        public IFieldValidatorBuilder Append(IFieldConstraint value)
        {
            constraint = constraint != null
                ? new CompositeFieldConstraint(constraint, value) 
                : value;
            return this;
        }

        public FieldValidator Build(JPath field)
        {
            return new FieldValidator(field, constraint);
        }
    }
}