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

        public override JObject BeforePost(dynamic entity, string contentType, PipelineContext context)
        {
            return Validate((JObject)entity, null, contentType, context);
        }

        public override JObject BeforePut(dynamic entity, dynamic prev, string contentType, PipelineContext context)
        {
            return Validate((JObject)entity, (JObject)prev, contentType, context);
        }

        private dynamic Validate(JObject entity, JObject prev, string contentType, PipelineContext context)
        {
            IValidator validator;
            if (validators.TryGetValue(contentType, out validator))
            {
                ValidationResult result = validator.Validate(entity, new ValidationContext(entity, prev, context, HttpVerbs.Put));
                if (result.HasErrors)
                {
                    throw new JsonEntityValidationException(result);
                }
            }
            return entity;
        }
    }
}