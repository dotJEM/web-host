using System;
using System.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PipelineDependency : Attribute
{
    public Type Type { get; set; }

    public PipelineDependency(Type other)
    {
        Type = other;
    }

    public PipelineDependency(string typeName)
    {
        Type = Type.GetType(typeName);
    }

    public static PipelineDependency[] GetDependencies(object handler)
    {
        Type type = handler.GetType();
        return type
            .GetCustomAttributes(typeof(PipelineDependency), true)
            .OfType<PipelineDependency>()
            .ToArray();
    }
}