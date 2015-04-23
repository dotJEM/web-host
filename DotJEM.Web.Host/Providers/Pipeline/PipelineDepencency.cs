using System;
using System.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
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