using System;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    [Obsolete]
    public interface IValidator
    {
        ValidationResult Validate(JObject entity, IValidationContext context);
    }
}