using System;
using System.Text.RegularExpressions;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
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
            return context.TryGetValue(key, out object value) && value is string str && filter.IsMatch(str);
        }
    }
}