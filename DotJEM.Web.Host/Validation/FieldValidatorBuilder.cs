using System;
using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Constraints;

namespace DotJEM.Web.Host.Validation
{
    [Obsolete]
    public class FieldValidatorBuilder : IFieldValidatorBuilder
    {
        private IFieldConstraint constraint;

        public IFieldValidatorBuilder Append(IFieldConstraint value)
        {
            constraint = constraint != null
                ? new CompositeFieldConstraint(constraint, value) 
                : value;
            return this;
        }

        public FieldValidator BuildValidator(JPath field)
        {
            return new FieldValidator(field, constraint);
        }

        public FieldGuard BuildGuard(JPath field)
        {
            return new FieldGuard(field, constraint);
        }
    }
}