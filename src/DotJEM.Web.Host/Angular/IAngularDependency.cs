namespace DotJEM.Web.Host.Angular;

public interface IAngularDependency
{
    string Name { get; }
    IAngularModule Module { get; }
}

internal class AngularDependency : IAngularDependency
{
    private IAngularDependencyState state;

    public string Name { get; private set; }

    public IAngularModule Module
    {
        get
        {
            return (state = state.Resolve()).Module;
        }
    }

    public AngularDependency(string name, AngularContext context)
    {
        Name = name;
        state = new UnresolvedState(name, context);
    }
}