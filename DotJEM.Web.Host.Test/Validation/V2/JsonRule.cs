using System.Collections.Generic;
using System.Linq;
using DotJEM.Web.Host.Validation;
using Newtonsoft.Json.Linq;

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

    public sealed class BasicJsonRule : JsonRule
    {
        private readonly string selector;
        private readonly JsonConstraint constraint;

        public BasicJsonRule(string selector, JsonConstraint constraint)
        {
            this.selector = selector;
            this.constraint = constraint.Optimize();
        }

        public override bool Test(IValidationContext contenxt, JObject entity)
        {
            JToken[] tokens = entity.SelectTokens(selector).ToArray();
            return tokens.All(token => constraint.Matches(contenxt, entity).Value);
        }
    }

    public abstract class CompositeJsonRule : JsonRule
    {
        public List<JsonRule> Rules { get; private set; }

        protected CompositeJsonRule(params JsonRule[] rules)
        {
            Rules = rules.ToList();
        }

        protected TRule OptimizeAs<TRule>() where TRule : CompositeJsonRule, new()
        {
            return Rules
                .Select(c => c.Optimize())
                .Aggregate(new TRule(), (c, next) =>
                {
                    TRule and = next as TRule;
                    if (and != null)
                    {
                        c.Rules.AddRange(and.Rules);
                    }
                    else
                    {
                        c.Rules.Add(next);
                    }
                    return c;
                });
        }
    }

    public sealed class AndJsonRule : CompositeJsonRule
    {
        public AndJsonRule()
        {
        }

        public AndJsonRule(params JsonRule[] rules) 
            : base(rules)
        {
        }

        public override bool Test(IValidationContext contenxt, JObject entity)
        {
            return Rules.All(rule => rule.Test(contenxt, entity));
        }

        public override JsonRule Optimize()
        {
            return OptimizeAs<AndJsonRule>();
        }
    }

    public sealed class OrJsonRule : CompositeJsonRule
    {
        public OrJsonRule()
        {
        }

        public OrJsonRule(params JsonRule[] rules)
            : base(rules)
        {
        }

        public override bool Test(IValidationContext contenxt, JObject entity)
        {
            return Rules.Any(rule => rule.Test(contenxt, entity));
        }

        public override JsonRule Optimize()
        {
            return OptimizeAs<OrJsonRule>();
        }
    }

    public sealed class NotJsonRule : JsonRule
    {
        public JsonRule Rule { get; private set; }

        public NotJsonRule(JsonRule rule)
        {
            Rule = rule;
        }

        public override bool Test(IValidationContext contenxt, JObject entity)
        {
            return !Rule.Test(contenxt, entity);
        }

        public override JsonRule Optimize()
        {
            NotJsonRule not = Rule as NotJsonRule;
            return not != null ? not.Rule : base.Optimize();
        }
    }
}