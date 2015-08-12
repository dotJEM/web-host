using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using DotJEM.Web.Host.Validation;
using DotJEM.Web.Host.Validation.Constraints;
using DotJEM.Web.Host.Validation.Results;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    [TestFixture]
    public class ValidationV2ConstraintBuilder
    {
        [Test]
        public void Validate_InvalidData_ShouldReturnErrors()
        {
            var constraint = (N & N) | (N & N & !N & N);

            string str = constraint.ToString();

            Assert.That(constraint, Is.TypeOf<OrJsonConstraint>());

            constraint = constraint.Optimize();

            string str2 = constraint.ToString();
            Assert.That(constraint, Is.TypeOf<OrJsonConstraint>());
        }

        public int counter = 1;

        public NamedJsonConstraint N
        {
            get { return new NamedJsonConstraint("" + counter++); }
        }

        [Test]
        public void SpecificValidator_InvalidData_ShouldReturnErrors()
        {
            SpecificValidator validator = new SpecificValidator();


            var result = validator.Validate(new JsonValidationContext(null, null), JObject.FromObject(new
            {
                test="01234567890123456789", other="0"
            }));

            Assert.That(result.IsValid, Is.True);
        }
    }

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

    public class SpecificValidator : JsonValidator
    {
        public SpecificValidator()
        {
            When("test", Is.LongerThan(5)).Then("test", Must.BeShorterThan(200));
            When("other", Is.LongerThan(0)).Then("test", Must.BeShorterThan(10));

            //When(Field("A", Is.Defined()) | Field("B", Is.Defined()))
            //    .Then(
            //          Field("A", Must.BeEqual("") | Must.BeEqual(""))
            //        & Field("B", Must.Not().BeEqual("")));
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

    public interface IJsonValidationContext
    {
        JObject Updated { get; }
        JObject Deleted { get; }
    }

    public class JsonValidationContext : IJsonValidationContext
    {
        public JObject Updated { get; private set; }
        public JObject Deleted { get; private set; }

        public JsonValidationContext(JObject updated, JObject deleted)
        {
            Updated = updated;
            Deleted = deleted;
        }
    }


    public class NamedJsonConstraint : JsonConstraint
    {
        public string Name { get; private set; }

        public NamedJsonConstraint(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            return True();
        }
    }

    public class ShorterJsonConstraint : JsonConstraint
    {
        private readonly int maxLength;

        public ShorterJsonConstraint(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length >= maxLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return False("Length must be less than '{0}'.", maxLength);
            }
            return True();
        }
    }

    public class LongerJsonConstraint : JsonConstraint
    {
        private readonly int minLength;

        public LongerJsonConstraint(int minLength)
        {
            this.minLength = minLength;
        }

        public override JsonConstraintResult Matches(IJsonValidationContext context, JToken token)
        {
            string value = (string)token;
            if (value.Length <= minLength)
            {
                //TODO: Provide Constraint Desciption instead.
                return False("Length must be longer than '{0}'.", minLength);
            }
            return True();
        }
    }


}
