using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotJEM.Web.Host.Angular
{
    public interface IAngularContext
    {
        IAngularContext Load(params string[] paths);
        IEnumerable<IAngularModule> Modules { get; }
        IEnumerable<string> Sources { get; }
        IAngularModule this[string name] { get; }
    }

    public class AngularContext : IAngularContext
    {
        private readonly AngularModuleFactory factory;
        private readonly IDictionary<string, IAngularModule> modules = new Dictionary<string, IAngularModule>();
        private readonly Lazy<IEnumerable<IAngularModule>> ordered;

        public IEnumerable<IAngularModule> Modules { get { return ordered.Value; } }
        public IEnumerable<string> Sources { get { return Modules.SelectMany(mod => mod.Sources); } }

        public AngularContext(params string[] paths)
        {
            factory = new AngularModuleFactory(this);
            ordered = new Lazy<IEnumerable<IAngularModule>>(() => OrderModules(modules));

            Load(paths);
        }

        public IAngularContext Load(params string[] paths)
        {
            foreach (string directory in paths.SelectMany(Directory.GetDirectories))
            {
                if (Directory.GetFiles(directory, "*.module.js").Any())
                {
                    LoadModule(directory);
                }
                else
                {
                    //TODO: We should support modules nested in modules.
                    Load(directory);
                }
            }
            return this;
        }

        private void LoadModule(string directory)
        {
            var module = factory.Load(directory);
            modules.Add(module.Name, module);
        }

        public IAngularModule this[string name]
        {
            get
            {
                IAngularModule module;
                modules.TryGetValue(name, out module);
                return module;
            }
        }

        private static IEnumerable<IAngularModule> OrderModules(IDictionary<string, IAngularModule> modules)
        {
            Queue<string> queue = new Queue<string>(modules.Keys);
            HashSet<string> ordered = new HashSet<string>();
            while (queue.Count > 0)
            {
                IAngularModule module = modules[queue.Dequeue()];
                if (module.Dependencies.All(x => ordered.Contains(x.Name) || !modules.ContainsKey(x.Name)))
                {
                    ordered.Add(module.Name);
                }
                else
                {
                    queue.Enqueue(module.Name);
                }
            }
            return ordered.Select(mod => modules[mod]).ToList();
        }
    }
}