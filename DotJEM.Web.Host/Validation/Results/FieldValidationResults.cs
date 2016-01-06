using System.Linq;
using System.Text;
using DotJEM.Json.Index.Schema;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Results
{
    public class FieldValidationResults
    {
        private readonly JPath field;
        private readonly JToken token;
        private readonly IValidationCollector collector;

        public FieldValidationResults(JPath field, JToken token, IValidationCollector collector)
        {
            this.field = field;
            this.token = token;
            this.collector = collector;
        }

        public bool HasErrors { get { return collector.HasErrors; } }

        public override string ToString()
        {
            //TODO: We could add information about field and token?
            string path = token != null ? token.Path : field.ToString();
            return collector
                .Aggregate(new StringBuilder().AppendFormat("Validation errors for: '{0}'", path).AppendLine(),
                    (b, error) => b.AppendLine(" - " + error.Format(field)))
                .AppendLine().ToString();
        }
    }
}