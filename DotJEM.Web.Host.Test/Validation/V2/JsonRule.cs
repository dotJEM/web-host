using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonRule
    {
        public abstract JsonRuleResult Test(IJsonValidationContext contenxt, JObject entity);

        public static AndJsonRule operator &(JsonRule x, JsonRule y)
        {
            return new AndJsonRule(x, y);
        }

        public static OrJsonRule operator |(JsonRule x, JsonRule y)
        {
            return new OrJsonRule(x, y);
        }

        public static NotJsonRule operator !(JsonRule x)
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
        private static readonly Regex arraySelector = new Regex(@".*\[\*|.+].*", RegexOptions.Compiled);

        private readonly string selector;
        private readonly JsonConstraint constraint;
        private bool hasArray;

        public BasicJsonRule(string selector, JsonConstraint constraint)
        {
            this.selector = selector;
            this.constraint = constraint.Optimize();
            this.hasArray = arraySelector.IsMatch(selector);
        }

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            return new AndJsonRuleResult(
                (from token in SelectTokens(entity)
                 select (JsonRuleResult)new BasicJsonRuleResult(token.Path, constraint.Matches(context, token))).ToList());
        }

        private IEnumerable<JToken> SelectTokens(JObject entity)
        {
            if (hasArray)
            {
                return entity.SelectTokens(selector).ToList();
            }
            return new[] { entity.SelectToken(selector) };
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

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            //TODO: Lazy
            return Rules.Aggregate(new AndJsonRuleResult(), (result, rule) => result & rule.Test(context, entity));
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

        public override JsonRuleResult Test(IJsonValidationContext context, JObject entity)
        {
            //TODO: Lazy
            return Rules.Aggregate(new OrJsonRuleResult(), (result, rule) => result | rule.Test(context, entity));
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

        public override JsonRuleResult Test(IJsonValidationContext contenxt, JObject entity)
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