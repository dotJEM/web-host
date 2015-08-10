using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Validation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Test.Validation.V2
{
    public abstract class JsonConstraintResult
    {
        public abstract bool Value { get; }
        public abstract string Message { get; }

        public virtual JsonConstraintResult Optimize()
        {
            return this;
        }
        
        public static implicit operator JsonConstraintResult(bool value)
        {
            return new BasicJsonConstraintResult(value, null, null);
        }

        public static JsonConstraintResult operator &(JsonConstraintResult x, JsonConstraintResult y)
        {
            return new AndJsonConstraintResult(x, y);
        }

        public static JsonConstraintResult operator |(JsonConstraintResult x, JsonConstraintResult y)
        {
            return new OrJsonConstraintResult(x, y);
        }

        public static JsonConstraintResult operator !(JsonConstraintResult x)
        {
            return new NotJsonConstraintResult(x);
        }

    }

    public sealed class BasicJsonConstraintResult : JsonConstraintResult
    {
        private readonly bool value;
        private readonly string message;

        public override bool Value
        {
            get { return value; }
        }

        public override string Message
        {
            get { return message; }
        }

        public Type ConstraintType { get; private set; }

        public BasicJsonConstraintResult(bool value, string message, Type constraintType)
        {
            this.value = value;
            this.message = message;
            ConstraintType = constraintType;
        }
    }

    public abstract class CompositeJsonConstraintResult : JsonConstraintResult
    {
        protected List<JsonConstraintResult> Results { get; private set; }

        public override string Message
        {
            get
            {
                return Results.Aggregate(new StringBuilder(), (builder, result) =>
                {
                    if (!result.Value)
                    {
                        builder.AppendLine(result.Message);
                    }
                    return builder;
                }).ToString();
            }
        }
        
        protected CompositeJsonConstraintResult(List<JsonConstraintResult> results)
        {
            Results = results;
        }

        protected TResult OptimizeAs<TResult>() where TResult : CompositeJsonConstraintResult, new()
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

    public sealed class AndJsonConstraintResult : CompositeJsonConstraintResult
    {
        public override bool Value
        {
            get { return Results.All(r => r.Value); }
        }

        public AndJsonConstraintResult() 
            : base(new List<JsonConstraintResult>())
        {
        }

        public AndJsonConstraintResult(params JsonConstraintResult[] results)
            : base(results.ToList())
        {
        }

        public AndJsonConstraintResult(List<JsonConstraintResult> results) 
            : base(results)
        {
        }

        public override JsonConstraintResult Optimize()
        {
            return OptimizeAs<AndJsonConstraintResult>();
        }
    }

    public sealed class OrJsonConstraintResult : CompositeJsonConstraintResult
    {
        public override bool Value
        {
            get { return Results.Any(r => r.Value); }
        }

        public OrJsonConstraintResult() 
            : base(new List<JsonConstraintResult>())
        {
        }

        public OrJsonConstraintResult(params JsonConstraintResult[] results)
            : base(results.ToList())
        {
        }

        public OrJsonConstraintResult(List<JsonConstraintResult> results) 
            : base(results)
        {
        }

        public override JsonConstraintResult Optimize()
        {
            return OptimizeAs<OrJsonConstraintResult>();
        }
    }

    public sealed class NotJsonConstraintResult : JsonConstraintResult
    {
        public JsonConstraintResult Result { get; private set; }

        public override bool Value
        {
            get { return !Result.Value; }
        }

        public override string Message
        {
            get { return Result.Message; }
        }

        public NotJsonConstraintResult(JsonConstraintResult result)
        {
            Result = result;
        }

        public override JsonConstraintResult Optimize()
        {
            NotJsonConstraintResult not = Result as NotJsonConstraintResult;
            return not != null ? not.Result : base.Optimize();
        }
    }
}
