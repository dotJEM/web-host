using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Web.Host.Validation;
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

            Assert.That(constraint, Is.TypeOf<OrJsonFieldConstraint>());

            constraint = constraint.Optimize();

            string str2 = constraint.ToString();
            Assert.That(constraint, Is.TypeOf<OrJsonFieldConstraint>());
        }

        public int counter = 1;

        public NamedJsonFieldConstraint N
        {
            get { return new NamedJsonFieldConstraint("" + counter++); }
        }
    }

    public interface IGuardConstraintFactory { }
    public interface IValidatorConstraintFactory { }

    public static class JsonGuardExtensions
    {
        public static JsonFieldConstraint Defined(this IGuardConstraintFactory self)
        {
            return null;
        }
    }
    public static class JsonValidatorExtensions
    {
        public static JsonFieldConstraint BeDefined(this IValidatorConstraintFactory self)
        {
            return null;
        }

        public static IValidatorConstraintFactory Not(this IValidatorConstraintFactory self)
        {
            return null;
        }

        public static JsonFieldConstraint BeEqual(this IValidatorConstraintFactory self, object value)
        {
            return null;
        }
    }

    public class JsonValidator
    {
        private List<JsonFieldValidator> fieldValidators = new List<JsonFieldValidator>(); 

        protected IGuardConstraintFactory Is { get; set; }
        protected IValidatorConstraintFactory Must { get; set; }
        protected IValidatorConstraintFactory Should { get; set; }

        protected IJsonValidatorRuleFactory When(JsonRule rule)
        {
            return new JsonValidatorRuleFactory(this, rule);
        }

        protected IJsonValidatorRuleFactory When(string selector, JsonFieldConstraint constraint)
        {
            return When(Field(selector, constraint));
        }

        protected JsonRule Field(string selector, JsonFieldConstraint constraint)
        {
            return new BasicJsonRule(selector, constraint);
        }

        public void AddFieldValidator(JsonFieldValidator jsonFieldValidator)
        {
            fieldValidators.Add(jsonFieldValidator);
        }

        public void Validate(IValidationContext contenxt, JObject entity)
        {
            
        }
    }

    public interface IJsonValidatorRuleFactory
    {
        void Then(JsonRule validator);
        void Then(string selector, JsonFieldConstraint validator);
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
            validator.AddFieldValidator(new JsonFieldValidator(rule));
        }

        public void Then(string selector, JsonFieldConstraint constraint)
        {
            Then(new BasicJsonRule(selector,constraint));
        }
    }

    public class SpecificValidator : JsonValidator
    {
        public SpecificValidator()
        {
            When("A", Is.Defined()).Then("A", Must.BeDefined());
            When(Field("A", Is.Defined()) | Field("B", Is.Defined()))
                .Then(
                      Field("A", Must.BeEqual("") | Must.BeEqual(""))
                    & Field("B", Must.Not().BeEqual("")));
        }

    }



    public class JsonFieldValidator
    {
        private readonly JsonRule rule;

        public JsonFieldValidator(JsonRule rule)
        {
            this.rule = rule;
        }

        public bool Validate(IValidationContext contenxt, JObject entity)
        {
            if (rule.Test(contenxt, entity))
                return true;

            return false;
        }
    }



}
