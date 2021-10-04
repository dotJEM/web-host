using System;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    public abstract class PipelineFilterAttribute : Attribute
    {
        public abstract string Group { get; }

        public abstract bool Accepts(IPipelineContext context);
    }
}