using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using DotJEM.Web.Host.Validation2.Rules;
using DotJEM.Web.Host.Validation2.Rules.Results;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2
{
    public interface IGuardConstraintFactory { }
    public interface IValidatorConstraintFactory { }

    public static class JsonGuardExtensions
    {
        public static JsonConstraint LongerThan(this IGuardConstraintFactory self, int length)
        {
            return new LongerJsonConstraint(length);
        }

        public static JsonConstraint ShorterThan(this IGuardConstraintFactory self, int length)
        {
            return new ShorterJsonConstraint(length);
        }
    }

    public static class JsonValidatorExtensions
    {
        public static JsonConstraint BeLongerThan(this IValidatorConstraintFactory self, int length)
        {
            return new LongerJsonConstraint(length);
        }

        public static JsonConstraint BeShorterThan(this IValidatorConstraintFactory self, int length)
        {
            return new ShorterJsonConstraint(length);
        }

        public static JsonConstraint BeDefined(this IValidatorConstraintFactory self)
        {
            return null;
        }

        public static IValidatorConstraintFactory Not(this IValidatorConstraintFactory self)
        {
            return null;
        }

        public static JsonConstraint BeEqual(this IValidatorConstraintFactory self, object value)
        {
            return null;
        }
    }

    public class JsonValidator
    {
        private readonly List<JsonFieldValidator> validators = new List<JsonFieldValidator>(); 

        protected IGuardConstraintFactory Is { get; set; }
        protected IValidatorConstraintFactory Must { get; set; }
        protected IValidatorConstraintFactory Should { get; set; }

        protected IJsonValidatorRuleFactory When(JsonRule rule)
        {
            return new JsonValidatorRuleFactory(this, rule);
        }

        protected IJsonValidatorRuleFactory When(string selector, JsonConstraint constraint)
        {
            return When(Field(selector, constraint));
        }

        protected JsonRule Field(string selector, JsonConstraint constraint)
        {
            return new BasicJsonRule(selector, constraint);
        }

        public void AddFieldValidator(JsonFieldValidator jsonFieldValidator)
        {
            validators.Add(jsonFieldValidator);
        }

        public JsonValidatorResult Validate(IJsonValidationContext contenxt, JObject entity)
        {
            IEnumerable<JsonRuleResult> results = from validator in validators
                                                  let result = validator.Validate(contenxt, entity)
                                                  where result != null
                                                  select result;
            return new JsonValidatorResult(results.ToList());
        }
    }

    public class JsonValidatorResult
    {
        private readonly List<JsonRuleResult> results;

        public bool IsValid { get { return results.All(r => r.Value); } }

        public JsonValidatorResult(List<JsonRuleResult> results)
        {
            this.results = results;
        }

        public string Describe()
        {
            return "";
        }
    }

    public interface IJsonValidatorRuleFactory
    {
        void Then(JsonRule validator);
        void Then(string selector, JsonConstraint validator);
    }

    public class JsonValidatorRuleFactory : IJsonValidatorRuleFactory
    {
        private readonly JsonRule rule;
        private readonly JsonValidator validator;

        public JsonValidatorRuleFactory(JsonValidator validator, JsonRule rule)
        {
            this.validator = validator;
            this.rule = rule;
        }

        public void Then(JsonRule rule)
        {
            validator.AddFieldValidator(new JsonFieldValidator(this.rule, rule));
        }

        public void Then(string selector, JsonConstraint constraint)
        {
            Then(new BasicJsonRule(selector,constraint));
        }
    }

    public class JsonFieldValidator
    {
        private readonly JsonRule guard;
        private readonly JsonRule rule;

        public JsonFieldValidator(JsonRule guard, JsonRule rule)
        {
            this.guard = guard.Optimize();
            this.rule = rule.Optimize();
        }

        public JsonRuleResult Validate(IJsonValidationContext context, JObject entity)
        {
            var gr = guard.Test(context, entity);
            if (!gr.Value)
                return null;

            return rule.Test(context, entity);
        }
    }

    [JsonConstraintDescription("Length must be longer than '{maxLength}'.")]
    public class ShorterJsonConstraint : JsonConstraint
    {
        private readonly int maxLength;

        public ShorterJsonConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public override bool Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length >= maxLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return false;
            }
            return true;
        }
    }

    [JsonConstraintDescription("Length must be longer than '{minLength}'.")]
    public class LongerJsonConstraint : JsonConstraint
    {
        private readonly int minLength;

        public LongerJsonConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        public override bool Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length <= minLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return false;
            }
            return true;
        }
    }


}
