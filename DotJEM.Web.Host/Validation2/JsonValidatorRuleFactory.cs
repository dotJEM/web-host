﻿using DotJEM.Web.Host.Validation2.Constraints;
using DotJEM.Web.Host.Validation2.Rules;

namespace DotJEM.Web.Host.Validation2
{
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
            validator.AddValidator(new JsonFieldValidator(this.rule, rule));
        }

        public void Then(string selector, JsonConstraint constraint)
        {
            Then(selector, selector,constraint);
        }

        public void Then(string selector, string alias, JsonConstraint constraint)
        {
            Then(new BasicJsonRule(selector, alias, constraint));
        }
    }
}