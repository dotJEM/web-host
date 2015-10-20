using System.Collections.Generic;
using System.Linq;
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

        public string Describe()
        {

            return "";
        }
    }
}