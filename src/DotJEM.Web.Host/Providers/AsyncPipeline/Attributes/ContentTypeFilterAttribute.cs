using System;
using System.Text.RegularExpressions;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : PropertyFilterAttribute
    {

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
            : base("contentType", regex, options)
        {
        }
    }
}