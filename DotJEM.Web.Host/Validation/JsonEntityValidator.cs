using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public class JsonEntityValidator : PipelineHandler
    {
        private readonly Dictionary<string, IValidator> validators;

        public JsonEntityValidator(IValidator[] validators)
        {
            this.validators = validators.ToDictionary(Validator.GetValidatorName);
        }

        public override JObject BeforePost(dynamic entity, string contentType)
        {
            return Validate(entity, contentType);
        }

        public override JObject BeforePut(dynamic entity, dynamic prev, string contentType)
        {
            return Validate(entity, prev, contentType);
        }

        private dynamic Validate(dynamic entity, string contentType)
        {
            IValidator validator;
            if (validators.TryGetValue(contentType, out validator))
            {
                ValidationResult result = validator.Validate((JObject)entity, new ValidationContext((JObject)entity, null, HttpVerbs.Post));
                if (result.HasErrors)
                {
                    throw new JsonEntityValidationException(result);
                }
            }
            return entity;
        }

        private dynamic Validate(dynamic entity, dynamic prev, string contentType)
        {
            IValidator validator;
            if (validators.TryGetValue(contentType, out validator))
            {
                ValidationResult result = validator.Validate((JObject)entity, new ValidationContext((JObject)entity, (JObject)prev, HttpVerbs.Put));
                if (result.HasErrors)
                {
                    throw new JsonEntityValidationException(result);
                }
            }
            return entity;
        }
    }
}