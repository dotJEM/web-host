using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    public abstract class PipelineFilterAttribute : Attribute
    {
        public abstract bool Accepts(IPipelineContext context);
    }
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PropertyFilterAttribute : PipelineFilterAttribute
    {
        private readonly string key;
        private readonly Regex filter;

        public PropertyFilterAttribute(string key, string regex, RegexOptions options = RegexOptions.None)
        {
            this.key = key;
            //NOTE: Force compiled.
            filter = new Regex(regex, options | RegexOptions.Compiled);
        }

        public override bool Accepts(IPipelineContext context)
        {
            return context.TryGetValue(key, out string value) && filter.IsMatch(value);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeFilterAttribute : PropertyFilterAttribute
    {

        public ContentTypeFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
            : base("contentType", regex, options)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HttpMethodFilterAttribute : PropertyFilterAttribute
    {

        public HttpMethodFilterAttribute(string regex, RegexOptions options = RegexOptions.None)
            : base("method", regex, options | RegexOptions.IgnoreCase)
        {
        }
    }



    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PipelineDepencency : Attribute
    {
        public Type Type { get; set; }

        public PipelineDepencency(Type other)
        {
            Type = other;
        }

        public static PipelineDepencency[] GetDepencencies(object handler)
        {
            Type type = handler.GetType();
            return type
                .GetCustomAttributes(typeof(PipelineDepencency), true)
                .OfType<PipelineDepencency>()
                .ToArray();
        }
    }
}