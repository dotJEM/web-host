using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public interface IFieldValidator
    {
        FieldValidationResults Validate(JObject entity);
    }
}