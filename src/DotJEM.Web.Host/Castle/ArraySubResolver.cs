using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace DotJEM.Web.Host.Castle
{
    public class ArraySubResolver : ISubDependencyResolver
    {
        private readonly IKernel kernel;

        public ArraySubResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
            ComponentModel model,
            DependencyModel dependency)
        {
            return kernel.ResolveAll(dependency.TargetType.GetElementType(), null);
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
            ComponentModel model,
            DependencyModel dependency)
        {
            return dependency.TargetType != null &&
                   dependency.TargetType.IsArray &&
                   dependency.TargetType.GetElementType().IsInterface;
        }
    }
}