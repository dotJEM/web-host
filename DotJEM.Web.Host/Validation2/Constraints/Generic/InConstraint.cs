using System.Collections.Generic;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;

namespace DotJEM.Web.Host.Validation2.Constraints.Generic
{
    [JsonConstraintDescription("length must be from '{minLength}' to '{maxLength}'.")]
    public class InConstraint<T> : TypedJsonConstraint<T>
    {
        private readonly HashSet<T> values;

        public InConstraint(IEnumerable<T> values)
        {
            this.values = new HashSet<T>(values, EqualityComparer<T>.Default);
        }

        protected override bool Matches(IJsonValidationContext context, T value)
        {
            return values.Contains(value);
        }
    }
   
}
