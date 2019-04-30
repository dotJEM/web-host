using System;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    [Obsolete]
    public interface IFieldConstraint
    {
        void Validate(IValidationContext context, JToken token, IValidationCollector collector);
        bool Matches(JToken token);
    }
}