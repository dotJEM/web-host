using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonRule
    {
        public abstract bool Test(IValidationContext contenxt, JObject entity);


        public static JsonRule operator &(JsonRule x, JsonRule y)
        {
            return new AndJsonRule(x, y);
        }

        public static JsonRule operator |(JsonRule x, JsonRule y)
        {
            return new OrJsonRule(x, y);
        }

        public static JsonRule operator !(JsonRule x)
        {
            return new NotJsonRule(x);
        }

        public virtual JsonRule Optimize()
        {
            return this;
        }


    }

    public class BasicJsonRule : JsonRule
    {
        private readonly string selector;
        private readonly JsonFieldConstraint constraint;

        public BasicJsonRule(string selector, JsonFieldConstraint constraint)
        {
            this.selector = selector;
            this.constraint = constraint;
        }

        public override bool Test(IValidationContext contenxt, JObject entity)
        {
            JToken[] tokens = entity.SelectTokens(selector).ToArray();
            return tokens.All(token => constraint.Matches(contenxt, entity));

            //List< JToken > tokens = entity.SelectTokens(field.Path).ToList();
            //if (tokens.Count > 1)
            //{
            //    foreach (JToken token in tokens)
            //        yield return ValidateToken(token, context);
            //}
            //else
            //{
            //    yield return ValidateToken(tokens.SingleOrDefault(), context);
            //}
        }
    }

    public class CompositeJsonRule : JsonRule
    {
        public List<JsonRule> Rules { get; private set; }

        public CompositeJsonRule(params JsonRule[] rules)
        {
            this.Rules = rules.ToList();
        }
    }

    public class AndJsonRule : CompositeJsonRule
    {
        public AndJsonRule(params JsonRule[] rules) 
            : base(rules)
        {
        }
    }

    public class OrJsonRule : CompositeJsonRule
    {
        public OrJsonRule(params JsonRule[] rules)
            : base(rules)
        {
        }
    }

    public class NotJsonRule : JsonRule
    {
        public JsonRule Rule { get; private set; }

        public NotJsonRule(JsonRule rule)
        {
            Rule = rule;
        }
    }
}