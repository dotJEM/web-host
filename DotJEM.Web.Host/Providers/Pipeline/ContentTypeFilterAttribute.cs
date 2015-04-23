using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : Attribute
    {
        public string Regex { get; set; }
        public bool Partial { get; set; }

        public ContentTypeFilterAttribute(string regex)
        {
            Regex = regex;
        }

        public static ContentTypeFilterAttribute[] GetFilters(object handler)
        {
            Type type = handler.GetType();
            return type
                .GetCustomAttributes(typeof(ContentTypeFilterAttribute), true)
                .OfType<ContentTypeFilterAttribute>()
                .ToArray();
        }

        public Regex BuildRegex()
        {
            return Partial
                ? new Regex(Regex, RegexOptions.Compiled)
                : new Regex("^" + Regex + "$", RegexOptions.Compiled);
        }
    }
}