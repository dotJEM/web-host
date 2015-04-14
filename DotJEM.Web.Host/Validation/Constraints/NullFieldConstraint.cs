using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{
    public class NullFieldConstraint : IFieldConstraint
    {
        public void Validate(JToken token, IValidationCollector collector)
        {
        }
    }
}