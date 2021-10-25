using System;
using System.Text.RegularExpressions;
using DotJEM.Pipelines.Attributes;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpMethodFilterAttribute : PropertyFilterAttribute
    {
        public HttpMethodFilterAttribute(string method, RegexOptions options = RegexOptions.None)
            : base("method", $"^{method}$", options | RegexOptions.IgnoreCase)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpGetMethodFilterAttribute : HttpMethodFilterAttribute
    {
        public HttpGetMethodFilterAttribute(RegexOptions options = RegexOptions.None)
            : base("GET", options)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostMethodFilterAttribute : HttpMethodFilterAttribute
    {
        public HttpPostMethodFilterAttribute(RegexOptions options = RegexOptions.None)
            : base("POST", options)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPutMethodFilterAttribute : HttpMethodFilterAttribute
    {
        public HttpPutMethodFilterAttribute(RegexOptions options = RegexOptions.None)
            : base("PUT", options)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPatchMethodFilterAttribute : HttpMethodFilterAttribute
    {
        public HttpPatchMethodFilterAttribute(RegexOptions options = RegexOptions.None)
            : base("PATCH", options)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteMethodFilterAttribute : HttpMethodFilterAttribute
    {
        public HttpDeleteMethodFilterAttribute(RegexOptions options = RegexOptions.None)
            : base("DELETE", options)
        {

        }
    }
}