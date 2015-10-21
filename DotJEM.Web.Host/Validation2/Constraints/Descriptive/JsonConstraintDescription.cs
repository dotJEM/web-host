using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;


namespace DotJEM.Web.Host.Validation2.Constraints.Descriptive
{
    public class JsonConstraintDescription
    {
        // { field or property, spacing : format }
        private static readonly Regex replacer = new Regex(@"\{\s*(?<field>\w+?(\.\w+)*)\s*(\,\s*(?<spacing>\-?\d+?))?\s*(\:\s*(?<format>.*?))?\s*\}", 
            RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly Type type;
        private readonly JsonConstraint source;
        private readonly string format;
        private readonly JToken token;

        public JsonConstraintDescription(JsonConstraint source, string format, JToken token)
        {
            this.source = source;
            this.format = format;
            this.token = token;

            type = source.GetType();
        }

        public override string ToString()
        {
            return replacer.Replace(format, GetValue);
        }

        private string GetValue(Match match)
        {
            string fieldOrProperty = match.Groups["field"].Value;

            if (fieldOrProperty.StartsWith("token"))
            {
                return Evaluate(fieldOrProperty);
            }
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

        private string Evaluate(string expression)
        {
            //TODO: We need a more "evaluating" aproach.
            //      One posibility would be to use Roslyn
            if (token == null)
                return "";

            if (expression == "token")
                return token.ToString();

            switch (token.Type)
            {
                case JTokenType.Array:
                    return ((JArray) token).Count.ToString();
                case JTokenType.String:
                    return ((string) token).Length.ToString();
                case JTokenType.Bytes:
                    return ((byte[]) token).Length.ToString();
                default:
                    return token.ToString();
            }

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