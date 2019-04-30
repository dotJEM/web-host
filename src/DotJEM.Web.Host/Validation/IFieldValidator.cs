using System;
using System.Collections.Generic;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    [Obsolete]
    public interface IFieldValidator
    {
        IEnumerable<FieldValidationResults> Validate(JObject entity, IValidationContext context);
    }
}