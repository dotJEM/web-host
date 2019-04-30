using System;
using System.Collections.Generic;

namespace DotJEM.Web.Host.Validation.Results
{
    [Obsolete]
    public interface IValidationCollector : IEnumerable<ValidationError>
    {
        IValidationCollector AddError(string format, params object[] args);

        bool HasErrors { get; }
    }
}