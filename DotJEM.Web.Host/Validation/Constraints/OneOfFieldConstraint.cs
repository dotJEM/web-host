using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Constraints
{

    //Todo: Make generic to also match e.g. int and float
    public class OneOfFieldConstraint : FieldConstraint
    {
        private readonly IEnumerable<string> strings;
        private StringComparison? stringComparison;

        public OneOfFieldConstraint(IEnumerable<string> strings, StringComparison stringComparison) : this(strings)
        {
            this.stringComparison = stringComparison;
        }

        public OneOfFieldConstraint(IEnumerable<string> strings)
        {
            this.strings = strings;
        }

        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            string value = (string)token;
            Func<string, bool> predicate = x => x.Equals(value);
            if (stringComparison.HasValue)
                predicate = x => x.Equals(value, stringComparison.Value);

            if (strings.Any(predicate))
                return;

            context.AddError("The text must be one of the following: {0}", string.Join(", ", strings));
        }
    }
}
