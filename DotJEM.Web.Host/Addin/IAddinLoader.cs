using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Addin
{
    public interface IAddinDescriptor
    {
        
    }

    public interface IAddinLoader
    {
    }

    public interface IAssemblyInspector
    {
        IAssemblyDescriptor Inspect(FileInfo assemblyPath, params DirectoryInfo[] dependencyLocations);
    }

    public class AssemblyInspector : IAssemblyInspector
    {
        protected readonly bool shadowCopy;

        public AssemblyInspector()
            : this(true)
        {
        }

        public AssemblyInspector(bool shadowCopy)
        {
            this.shadowCopy = shadowCopy;
        }

        public IAssemblyDescriptor Inspect(FileInfo assemblyPath, params DirectoryInfo[] dependencyLocations)
        {
            using (InspectionAppDomain domain = new InspectionAppDomain(assemblyPath, shadowCopy))
            {
                //TODO: Cache Invalidation Like this may fail later call's to type.
                //Caching.ResourceCache.Default.Invalidate();
                Array.ForEach(dependencyLocations, domain.AddDependencyLocation);
                AssemblyDescriptor descriptor = domain.Loader.LoadAssembly(assemblyPath, new LoadInfo(shadowCopy, dependencyLocations));
                Caching.ResourceCache.Default.Store(descriptor);
                return descriptor;
            }
        }
    }
}
