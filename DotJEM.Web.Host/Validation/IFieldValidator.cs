using System.Collections.Generic;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public interface IFieldValidator
    {
        IEnumerable<FieldValidationResults> Validate(JObject entity);
    }
}