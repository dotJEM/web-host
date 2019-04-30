using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Angular
{
    public interface IAngularProject
    {
        string Name { get; }
        IEnumerable<IAngularModule> Modules { get; }
        IEnumerable<string> Sources { get; }

        IAngularModule this[string name] { get; }
    }

    internal class AngularProject : IAngularProject
    {
        private readonly IDictionary<string, IAngularModule> modules;
        private readonly Lazy<IEnumerable<IAngularModule>> ordered;

        public string Name { get; private set; }
        public IEnumerable<IAngularModule> Modules { get { return ordered.Value; } }
        public IEnumerable<string> Sources { get { return Modules.SelectMany(mod => mod.Sources); } }

        public IAngularModule this[string name]
        {
            get
            {
                IAngularModule module;
                modules.TryGetValue(name, out module);
                return module;
            }
        }

        public AngularProject(string name, IDictionary<string, IAngularModule> modules)
        {
            Name = name;
            ordered = new Lazy<IEnumerable<IAngularModule>>(() => OrderModules(modules));

            this.modules = modules;
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