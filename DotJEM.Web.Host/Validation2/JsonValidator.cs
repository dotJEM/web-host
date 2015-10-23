using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Descriptive;
using DotJEM.Web.Host.Validation2.Factories;
using DotJEM.Web.Host.Validation2.Rules;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2
{
    public interface IJsonValidator
    {
        JsonValidatorResult Validate(IJsonValidationContext contenxt, JObject entity);
        JsonValidatorDescription Describe();
    }

    public class JsonValidator : IJsonValidator
    {
        private readonly List<JsonFieldValidator> validators = new List<JsonFieldValidator>();
        protected IGuardConstraintFactory Is { get; } = new ConstraintFactory();
        protected IGuardConstraintFactory Has { get; } = new ConstraintFactory();
        protected IValidatorConstraintFactory Must { get; } = new ValidatorConstraintFactory();
        protected IValidatorConstraintFactory Should { get; } = new ValidatorConstraintFactory();

        protected IJsonValidatorRuleFactory When(JsonRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            return new JsonValidatorRuleFactory(this, rule);
        }

        protected IJsonValidatorRuleFactory When(string selector, JsonConstraint constraint)
        {
            return When(Field(selector, constraint));
        }

        protected IJsonValidatorRuleFactory When(string selector, string alias, JsonConstraint constraint)
        {
            return When(Field(selector, alias, constraint));
        }

        protected JsonRule Field(string selector, JsonConstraint constraint)
        {
            return Field(selector, selector, constraint);
        }

        protected JsonRule Field(string selector, string alias, JsonConstraint constraint)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));
            if (alias == null) throw new ArgumentNullException(nameof(alias));

            return new BasicJsonRule(selector, alias, constraint);
        }


        internal void AddValidator(JsonFieldValidator jsonFieldValidator)
        {
            validators.Add(jsonFieldValidator);
        }

        public JsonValidatorResult Validate(IJsonValidationContext contenxt, JObject entity)
        {
            IEnumerable<JsonRuleResult> results = from validator in validators
                let result = validator.Validate(contenxt, entity)
                where result != null
                select result.Optimize();
            return new JsonValidatorResult(results.ToList());
        }
        
        public JsonValidatorDescription Describe()
        {
            IEnumerable<JsonFieldValidatorDescription> descriptions = from validator in validators
                               select validator.Describe();

            return new JsonValidatorDescription(this, descriptions.ToList());

        }
    }
}