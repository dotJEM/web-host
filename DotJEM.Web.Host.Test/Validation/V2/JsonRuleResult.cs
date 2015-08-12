using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Validation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonRuleResult
    {
        public abstract bool Value { get; }

        public virtual JsonRuleResult Optimize()
        {
            return this;
        }

        public static AndJsonRuleResult operator &(JsonRuleResult x, JsonRuleResult y)
        {
            return new AndJsonRuleResult(x, y);
        }

        public static OrJsonRuleResult operator |(JsonRuleResult x, JsonRuleResult y)
        {
            return new OrJsonRuleResult(x, y);
        }

        public static NotJsonRuleResult operator !(JsonRuleResult x)
        {
            return new NotJsonRuleResult(x);
        }

    }

    public sealed class BasicJsonRuleResult : JsonRuleResult
    {
        private readonly JsonConstraintResult result;

        public override bool Value { get { return result.Value; } }
        public string Path { get; private set; }

        public BasicJsonRuleResult(string path, JsonConstraintResult result)
        {
            Path = path;
            this.result = result.Optimize();
        }
    }

    public abstract class CompositeJsonRuleResult : JsonRuleResult
    {
        protected List<JsonRuleResult> Results { get; private set; }
        
        protected CompositeJsonRuleResult(List<JsonRuleResult> results)
        {
            Results = results;
        }

        protected TResult OptimizeAs<TResult>() where TResult : CompositeJsonRuleResult, new()
        {
            return Results
                .Select(c => c.Optimize())
                .Aggregate(new TResult(), (c, next) =>
                {
                    TResult and = next as TResult;
                    if (and != null)
                    {
                        c.Results.AddRange(and.Results);
                    }
                    else
                    {
                        c.Results.Add(next);
                    }
                    return c;
                });
        }
    }

    public sealed class AndJsonRuleResult : CompositeJsonRuleResult
    {
        public override bool Value
        {
            get { return Results.All(r => r.Value); }
        }

        public AndJsonRuleResult() 
            : base(new List<JsonRuleResult>())
        {
        }

        public AndJsonRuleResult(params JsonRuleResult[] results)
            : base(results.ToList())
        {
        }

        public AndJsonRuleResult(List<JsonRuleResult> results) 
            : base(results)
        {
        }

        public override JsonRuleResult Optimize()
        {
            return OptimizeAs<AndJsonRuleResult>();
        }
    }

    public sealed class OrJsonRuleResult : CompositeJsonRuleResult
    {
        public override bool Value
        {
            get { return Results.Any(r => r.Value); }
        }

        public OrJsonRuleResult() 
            : base(new List<JsonRuleResult>())
        {
        }

        public OrJsonRuleResult(params JsonRuleResult[] results)
            : base(results.ToList())
        {
        }

        public OrJsonRuleResult(List<JsonRuleResult> results) 
            : base(results)
        {
        }

        public override JsonRuleResult Optimize()
        {
            return OptimizeAs<OrJsonRuleResult>();
        }
    }

    public sealed class NotJsonRuleResult : JsonRuleResult
    {
        public JsonRuleResult Result { get; private set; }

        public override bool Value
        {
            get { return !Result.Value; }
        }

        public NotJsonRuleResult(JsonRuleResult result)
        {
            Result = result;
        }

        public override JsonRuleResult Optimize()
        {
            NotJsonRuleResult not = Result as NotJsonRuleResult;
            return not != null ? not.Result : base.Optimize();
        }
    }
}
