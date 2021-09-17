using System;
using System.Text.RegularExpressions;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpMethodFilterAttribute : PropertyFilterAttribute
    {

        public HttpMethodFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
            : base("method", regex, options | RegexOptions.IgnoreCase)
        {
        }
    }
}