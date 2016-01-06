using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Validation.Results
{
    public class ValidationCollector : IValidationCollector
    {
        private readonly List<ValidationError> errors = new List<ValidationError>();
      
        public bool HasErrors => errors.Any();

        public IValidationCollector AddError(string format, params object[] args)
        {
            errors.Add(new ValidationError(format, args));
            return this;
        }

        public IEnumerator<ValidationError> GetEnumerator()
        {
            return errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}