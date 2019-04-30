namespace DotJEM.Web.Host.Angular
{
    public interface IAngularDependencyState
    {
        IAngularDependencyState Resolve();
        IAngularModule Module { get; }
    }
    internal class UnresolvedState : IAngularDependencyState
    {
        private readonly AngularContext context;

        public string Name { get; private set; }
        public IAngularModule Module { get { return new UnresolvedAngularModule(Name); } }

        public UnresolvedState(string name, AngularContext context)
        {
            this.Name = name;
            this.context = context;
        }

        public IAngularDependencyState Resolve()
        {
            IAngularModule module = context[Name];
            return module != null ? (IAngularDependencyState)new ResolvedState(module) : this;
        }
    }

    internal class ResolvedState : IAngularDependencyState
    {
        public IAngularModule Module { get; private set; }

        public ResolvedState(IAngularModule module)
        {
            Module = module;
        }

        public IAngularDependencyState Resolve()
        {
            return this;
        }
    }
}