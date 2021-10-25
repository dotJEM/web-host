using System;
using System.Text.RegularExpressions;
using DotJEM.Pipelines.Attributes;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : PropertyFilterAttribute
    {

        public ContentTypeFilterAttribute(string contentType, RegexOptions options = RegexOptions.None)
            : base("contentType", $"^{contentType}$", options)
        {
        }
    }
}