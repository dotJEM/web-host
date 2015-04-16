using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public abstract class Validator : IValidator
    {
        private readonly Lazy<string> contentType;
        private readonly Dictionary<string, FieldValidator> fieldValidators = new Dictionary<string, FieldValidator>();
        private IEnumerable<FieldValidator> Validators { get { return fieldValidators.Values; } }

        public string ContentType { get { return contentType.Value; } }

        //NOTE: These are just for a more natural feel (fluent)...
        protected IFieldValidatorBuilder Is { get { return new FieldValidatorBuilder(); } }
        protected IFieldValidatorBuilder Must { get { return new FieldValidatorBuilder(); } }

        protected Validator()
        {
            contentType = new Lazy<string>(() => GetValidatorName(this));
        }

        protected void Field(JPath field, IFieldValidatorBuilder builder)
        {
            fieldValidators.Add(field.ToString(), builder.Build(field));
        }

        public ValidationResult Validate(JObject entity)
        {
            //TODO: Need a better concept for this, specifically a "Dependant field" concept, that can also be used in cross validation.
            return RequiresValidation(entity)
                ? new ValidationResult(ContentType, entity, Validators.SelectMany(v => v.Validate(entity)).ToList())
                : new ValidationResult(ContentType, entity, Enumerable.Empty<FieldValidationResults>().ToList());
        }

        protected virtual bool RequiresValidation(JObject entity)
        {
            return true;
        }

        public  static string GetValidatorName(object obj)
        {
            Type type = obj.GetType();
            ValidatorAttribute definition = type
                .GetCustomAttributes(typeof(ValidatorAttribute), false)
                .OfType<ValidatorAttribute>()
                .SingleOrDefault();
            return definition == null ? type.Name.Replace("Validator", "") : definition.ContentType;
        }
    }
}
