using System;
using System.Linq;
using System.Text.RegularExpressions;
using Castle.Core.Internal;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{


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
            return type.GetAttributes<ContentTypeAttribute>().ToArray();
        }
    }
}