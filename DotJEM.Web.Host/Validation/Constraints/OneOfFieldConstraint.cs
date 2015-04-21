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
        private readonly HashSet<string> strings;

        public OneOfFieldConstraint(IEnumerable<string> strings)
            : this(strings, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public OneOfFieldConstraint(IEnumerable<string> strings, StringComparer comparer)
        {
            this.strings = new HashSet<string>(strings, comparer);
        }
        protected override void OnValidate(JToken token, IValidationCollector context)
        {
            if (Matches(token))
                return;

            context.AddError("The text must be one of the following: {0}", string.Join(", ", strings));
        }

        protected override bool OnMatches(JToken token)
        {
            string value = (string)token;
            return strings.Contains(value);
        }
    }
}
