using System;
using System.ComponentModel.DataAnnotations;
using ValidationResult = DotJEM.Web.Host.Validation.Results.ValidationResult;

namespace DotJEM.Web.Host.Validation
{
    [Obsolete]
    public class JsonEntityValidationException : Exception
    {
        public ValidationResult Result { get; private set; }

        //TODO: Simple implementation for now.
        public JsonEntityValidationException(ValidationResult result)
            : base(result.ToString())
        {
            Result = result;
        }
    }
}