using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Web.Host.Validation2.Constraints;

namespace DotJEM.Web.Host.Validation2.Descriptive
{
    public class JsonValidatorDescription
    {
        private readonly JsonValidator validator;
        private readonly List<JsonFieldValidatorDescription> descriptions;

        public JsonValidatorDescription(JsonValidator validator, List<JsonFieldValidatorDescription> descriptions)
        {
            this.validator = validator;
            this.descriptions = descriptions;
        }

        public override string ToString()
        {
            StringWriter writer = new StringWriter();

            descriptions.ForEach(d => writer.WriteLine(d.ToString()));

            return writer.ToString();
        }
    }

    public class JsonFieldValidatorDescription
    {
        private readonly IDescription rule;
        private readonly IDescription guard;

        public JsonFieldValidatorDescription(IDescription guard, IDescription rule)
        {
            this.rule = rule;
            this.guard = guard;
        }


        public override string ToString()
        {
            return $"When {guard} then {rule}";
        }
    }

    public abstract class JsonRuleDescription : IDescription
    {
        public abstract IDescriptionWriter WriteTo(IDescriptionWriter writer);

        public override string ToString()
        {
            return WriteTo(new DescriptionWriter()).ToString();
        }
    }

    public class BasicJsonRuleDescription : JsonRuleDescription
    {
        private readonly string alias;
        private readonly string selector;
        private readonly JsonConstraint constraint;

        public BasicJsonRuleDescription(string alias, string selector, JsonConstraint constraint)
        {
            this.alias = alias;
            this.selector = selector;
            this.constraint = constraint;
        }

        public override IDescriptionWriter WriteTo(IDescriptionWriter writer)
        {
            return writer.WriteLine($"{alias} should {constraint.Describe()}");
        }
    }

    public class JsonNotRuleDescription : JsonRuleDescription
    {
        private readonly JsonRuleDescription inner;

        public JsonNotRuleDescription(JsonRuleDescription inner)
        {
            this.inner = inner;
        }

        public override IDescriptionWriter WriteTo(IDescriptionWriter writer)
        {
            return writer.WriteLine($"not {inner}");
        }
    }

    public class CompositeJsonRuleDescription : JsonRuleDescription
    {
        private readonly IEnumerable<JsonRuleDescription> list;
        private readonly string @join;

        public CompositeJsonRuleDescription(IEnumerable<JsonRuleDescription> list, string join)
        {
            this.list = list;
            this.@join = @join;
        }

        public override IDescriptionWriter WriteTo(IDescriptionWriter writer)
        {
            using (writer.Indent())
            {
                list.Aggregate(false, (joining, description) =>
                {
                    if (joining)
                        writer.Write(join + " ");
                    description.WriteTo(writer);
                    return true;
                });
            }
            return writer;
        }
    }

    public interface IDescribable
    {
        IDescription Describe();
    }

    public interface IDescription
    {
        IDescriptionWriter WriteTo(IDescriptionWriter writer);
    }

    public interface IDescriptionWriter
    {
        IDisposable Indent();
        IDescriptionWriter Write(string format, params object[] arg);
        IDescriptionWriter WriteLine(string format, params object[] arg);
        IDescriptionWriter WriteLine();
    }

    public class DescriptionWriter : IDescriptionWriter
    {
        private int indentation;
        private readonly TextWriter inner;

        public DescriptionWriter()
            : this(new StringWriter())
        {
        }

        public DescriptionWriter(TextWriter inner)
        {
            this.inner = inner;
        }

        public IDisposable Indent()
        {
            return new IndentationScope(this);
        }

        public IDescriptionWriter Write(string format, params object[] arg)
        {
            format = new string(' ', indentation) + format;
            inner.Write(format, arg);
            return this;
        }

        public IDescriptionWriter WriteLine(string format, params object[] arg)
        {
            format = new string(' ', indentation) + format;
            inner.WriteLine(format, arg);
            return this;
        }

        public IDescriptionWriter WriteLine()
        {
            inner.WriteLine();
            return this;
        }

        public override string ToString()
        {
            return inner.ToString();
        }

        private sealed class IndentationScope : IDisposable
        {
            private readonly DescriptionWriter writer;

            public IndentationScope(DescriptionWriter writer)
            {
                this.writer = writer;
                writer.indentation++;
            }

            public void Dispose()
            {
                writer.indentation--;
            }
        }
    }
}