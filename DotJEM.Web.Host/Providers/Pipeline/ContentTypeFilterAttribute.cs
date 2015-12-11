using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Castle.Core.Internal;

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


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContentTypeAttribute : Attribute
    {
        public string ContentType { get; }

        public ContentTypeAttribute(string contentType)
        {
            this.ContentType = contentType;
        }

        public static ContentTypeAttribute[] GetAttributes<T>()
        {
            return GetAttributes(typeof(T));
        }

        public static ContentTypeAttribute[] GetAttributes(Type type)
        {
            return type.GetAttributes<ContentTypeAttribute>();
        }
    }
}