using System;

namespace DotJEM.Web.Host.Providers.AsyncPipeline.Attributes
{
    public abstract class PipelineFilterAttribute : Attribute
    {
        public abstract bool Accepts(IPipelineContext context);
    }
}