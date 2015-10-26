using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Web.Host.Validation2.Descriptive;
using DotJEM.Web.Host.Validation2.Rules.Results;

namespace DotJEM.Web.Host.Validation2
{
    public class JsonValidatorResult
    {
        private readonly List<JsonRuleResult> results;

        public bool IsValid
        {
            get { return results.All(r => r.Value); }
        }

        public JsonValidatorResult(List<JsonRuleResult> results)
        {
            this.results = results;
        }

        public JsonValidatorResultDescription Describe()
        {


            return new JsonValidatorResultDescription(results.Where(r => r.Value));
        }
    }
}