using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Validation2.Constraints.Results;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints
{
    public class JsonConstraintDescriptionAttribute : Attribute
    {
        public string Format { get; private set; }

        public JsonConstraintDescriptionAttribute(string format)
        {
            Format = format;
        }
    }

    public abstract class JsonConstraint
    {
        public JsonConstraintDescriptionAttribute description;

        protected JsonConstraint()
        {
            description = GetType()
                .GetCustomAttributes(typeof (JsonConstraintDescriptionAttribute), false)
                .OfType<JsonConstraintDescriptionAttribute>()
                .SingleOrDefault();
        }

        public abstract JsonConstraintResult Matches(IJsonValidationContext context, JToken token);

        public virtual JsonConstraintDescription Describe(IJsonValidationContext context, JToken token)
        {
            return new JsonConstraintDescription(this, description.Format);
        }

        protected JsonConstraintResult True()
        {
            return new BasicJsonConstraintResult(true, null, GetType());
        }

        protected JsonConstraintResult True(string format, params object[] args)
        {
            return new BasicJsonConstraintResult(false, string.Format(format, args), GetType());
        }

        protected JsonConstraintResult False()
        {
            return new BasicJsonConstraintResult(false, null, GetType());
        }

        protected JsonConstraintResult False(string format, params object[] args)
        {
            return new BasicJsonConstraintResult(false, string.Format(format, args), GetType());
        }

        public virtual JsonConstraint Optimize()
        {
            return this;
        }

        #region Operator Overloads
        public static JsonConstraint operator &(JsonConstraint x, JsonConstraint y)
        {
            return new AndJsonConstraint(x, y);
        }

        public static JsonConstraint operator |(JsonConstraint x, JsonConstraint y)
        {
            return new OrJsonConstraint(x, y);
        }

        public static JsonConstraint operator !(JsonConstraint x)
        {
            return new NotJsonConstraint(x);
        }
        #endregion
    }

    public class JsonConstraintDescription
    {
        // { field or property, spacing : format }
        private static readonly Regex replacer = new Regex(@"\{\s*(?<field>\w+?)\s*(\,\s*(?<spacing>\-?\d+?))?\s*(\:\s*(?<format>.*?))?\s*\}", 
            RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly Type type;
        private readonly JsonConstraint source;
        private readonly string format;

        public JsonConstraintDescription(JsonConstraint source, string format)
        {
            this.source = source;
            this.format = format;

            this.type = source.GetType();
        }

        public override string ToString()
        {
            return replacer.Replace(format, GetValue);
        }

        private string GetValue(Match match)
        {
            string fieldOrProperty = match.Groups["field"].Value;
            string format = BuildFormat(match);

            FieldInfo field = type.GetField(fieldOrProperty, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return string.Format(format, field.GetValue(source));
            }

            PropertyInfo property = type.GetProperty(fieldOrProperty, BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                return string.Format(format, property.GetValue(source));
            }

            return "(UNKNOWN FIELD OR PROPERTY)";
        }

        private static string BuildFormat(Match match)
        {
            string spacing = match.Groups["spacing"].Value;
            string format = match.Groups["format"].Value;

            StringBuilder builder = new StringBuilder(64);
            builder.Append("{0");
            if (!string.IsNullOrEmpty(spacing))
            {
                builder.Append(",");
                builder.Append(spacing);
            }
            if (!string.IsNullOrEmpty(format))
            {
                builder.Append(":");
                builder.Append(format);
            }
            builder.Append("}");
            return builder.ToString();
        }
    }
}