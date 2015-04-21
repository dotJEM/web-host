using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Constraints;

namespace DotJEM.Web.Host.Validation
{
    public interface IFieldValidatorBuilder
    {
        IFieldValidatorBuilder Append(IFieldConstraint value);
        FieldValidator BuildValidator(JPath field);
        FieldGuard BuildGuard(JPath field);
    }
}