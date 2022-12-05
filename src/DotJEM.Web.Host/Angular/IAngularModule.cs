using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Angular;

public interface IAngularModule
{
    string Name { get; }
    IEnumerable<IAngularDependency> Dependencies { get; }
    IEnumerable<string> Sources { get; }
}

internal class AngularModule : IAngularModule
{
    public string Name { get; private set; }
    public IEnumerable<IAngularDependency> Dependencies { get; private set; }
    public IEnumerable<string> Sources { get; private set; }

    public AngularModule(string name, IEnumerable<string> sources, IEnumerable<IAngularDependency> dependencies)
    {
        Name = name;
        Dependencies = dependencies;
        Sources = sources;
    }
}

internal class ExternalAngularModule : AngularModule
{
    public ExternalAngularModule(string name, IEnumerable<IAngularDependency> dependencies)
        : base(name, Enumerable.Empty<string>(), dependencies)
    {
    }
}

internal class UnresolvedAngularModule : AngularModule
{
    public UnresolvedAngularModule(string name)
        : base(name, Enumerable.Empty<string>(), Enumerable.Empty<IAngularDependency>())
    {
    }
}