using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation.Results
{
    public sealed class ValidationResult
    {
        private readonly List<FieldValidationResults> fieldsResults;

        public JObject Entity { get; private set; }
        public string ValidatorContentType { get; private set; }

        public IList<FieldValidationResults> FieldsResults
        {
            get { return fieldsResults.AsReadOnly(); }
        }

        public bool HasErrors
        {
            get { return fieldsResults.Any(fr => fr.HasErrors); }
        }

        public ValidationResult(string validatorContentType, JObject entity, List<FieldValidationResults> fieldsResults)
        {
            this.ValidatorContentType = validatorContentType;
            this.Entity = entity;
            this.fieldsResults = fieldsResults;
        }

        public override string ToString()
        {
            //TODO: We could add information about field and entity?
            return fieldsResults
                .Where(result => result.HasErrors)
                .Aggregate(new StringBuilder(), (builder, result) => builder.AppendLine(result.ToString()))
                .ToString();
        }

        public void Add(FieldValidationResults item)
        {
            fieldsResults.Add(item);
        }
    }
}