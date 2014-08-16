using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Angular
{
    public static class Angular
    {
        public static readonly IAngularContext Current = new AngularContext();

        public static IEnumerable<IAngularModule> Ordered { get { return Current.Ordered; } }

        public static IAngularContext LoadModule(string path)
        {
            return Current.LoadModule(path);
        }

        public static IAngularContext AddModule(string name, params string[] dependencies)
        {
            return Current.AddExternalModule(name, dependencies);
        }
    }

    public interface IAngularContext
    {
        IEnumerable<IAngularModule> Ordered { get; }

        IAngularModule Lookup(string name);

        IAngularContext LoadModule(string path);
        IAngularContext AddExternalModule(string name, string[] dependencies);
    }

    public class AngularContext : IAngularContext
    {
        private readonly AngularModuleFactory factory;
        private readonly IDictionary<string, IAngularModule> modules = new Dictionary<string, IAngularModule>();

        public AngularContext()
        {
            factory = new AngularModuleFactory(this);
        }

        public IAngularModule Lookup(string name)
        {
            IAngularModule module;
            modules.TryGetValue(name, out module);
            return module;
        }

        public IAngularContext LoadModule(string path)
        {
            IAngularModule module = factory.Load(path);
            modules[module.Name] = module;
            return this;
        }

        public IAngularContext AddExternalModule(string name, string[] dependencies)
        {
            modules.Add(name, new ExternalAngularModule(name, dependencies.Select(n => new AngularDependency(n, this))));
            return this;
        }

        public IEnumerable<IAngularModule> Ordered
        {
            get
            {
                Queue<string> queue = new Queue<string>(modules.Keys);
                HashSet<string> ordered = new HashSet<string>();
                while (queue.Count > 0)
                {
                    IAngularModule module = modules[queue.Dequeue()];

                    if (module.Dependencies.All(x => ordered.Contains(x.Name) || !modules.ContainsKey(x.Name)))
                        ordered.Add(module.Name);
                    else
                        queue.Enqueue(module.Name);
                }
                return ordered.Select(mod => modules[mod]).ToList();
            }
        }
    }

    public class AngularModuleFactory
    {
        public static Regex Module = new Regex(@"angular\.module\s*\((?<definition>.*?\[.*?\].*?)\)", RegexOptions.Compiled);
        public static Regex Definition = new Regex(@"('(?<name>.+?)'|""(?<name>.+?)"")\s*,\s*\[(?<dependencies>.*?)\]", RegexOptions.Compiled);
        public static Regex Dependencies = new Regex(@"('(?<name>.+?)'|""(?<name>.+?)"")", RegexOptions.Compiled);

        private readonly AngularContext context;

        public AngularModuleFactory(AngularContext context)
        {
            this.context = context;
        }

        public IAngularModule Load(string path)
        {
            HashSet<string> scripts = SortScripts(path);
            string definition = ExtractDefinition(LoadSource(scripts.First()));
            Match match = Definition.Match(definition);
            string[] dependencies = ParseDependencies(match.Groups["dependencies"].ToString()).ToArray();

            return new AngularModule(
                match.Groups["name"].ToString(),
                scripts,
                dependencies.Select(name => new AngularDependency(name, context)));
        }

        private static HashSet<string> SortScripts(string path)
        {
            HashSet<string> sources = new HashSet<string>();
            IEnumerable<string> scripts = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories);
            sources.Add(scripts.Single(script => script.EndsWith(".module.js")));
            scripts.ForEach(script => sources.Add(script));
            return sources;
        }


        private static string LoadSource(string module)
        {
            string source = File.ReadAllLines(module).Aggregate((aggregate, next) => aggregate + " " + next);
            return Regex.Replace(source, "\\s+", " ");
        }

        private static string ExtractDefinition(string source)
        {
            return Module.Match(source).Groups["definition"].ToString().Trim();
        }

        private static IEnumerable<string> ParseDependencies(string dependencyString)
        {
            return from Match dependency in Dependencies.Matches(dependencyString)
                   select dependency.Groups["name"].ToString();
        }
    }

    public interface IAngularModule
    {
        string Name { get; }
        IEnumerable<IAngularDependency> Dependencies { get; }
        IEnumerable<string> Sources { get; }
    }

    public class AngularModule : IAngularModule
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

    public class ExternalAngularModule : AngularModule
    {
        public ExternalAngularModule(string name, IEnumerable<IAngularDependency> dependencies)
            : base(name, Enumerable.Empty<string>(), dependencies)
        {
        }
    }

    public class UnresolvedAngularModule : AngularModule
    {
        public UnresolvedAngularModule(string name)
            : base(name, Enumerable.Empty<string>(), Enumerable.Empty<IAngularDependency>())
        {
        }
    }

    public interface IAngularDependency
    {
        string Name { get; }
        IAngularModule Module { get; }
    }

    public class AngularDependency : IAngularDependency
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

    public class UnresolvedState : IAngularDependencyState
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
            IAngularModule module = context.Lookup(Name);
            return module != null ? (IAngularDependencyState)new ResolvedState(module) : this;
        }
    }

    public class ResolvedState : IAngularDependencyState
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

    public interface IAngularDependencyState
    {
        IAngularDependencyState Resolve();
        IAngularModule Module { get; }
    }


    //angular.module('dotjem', ['dotjem.core', 'dotjem.common', 'dotjem.plugin.blog', 'dotjem.routing', 'dotjem.angular.tree']);



    //<script src="assets/scripts/core/dotjem.core.module.js"></script>
    //<script src="assets/scripts/common/dotjem.common.module.js"></script>


    //<script src="assets/plugins/blog/scripts/dotjem.plugin.blog.module.js"></script>

    //<script src="assets/scripts/dotjem/dotjem.module.js"></script>
    //<script src="assets/scripts/dotjem/controllers/appController.js"></script>
    //<script src="assets/scripts/dotjem/controllers/authController.js"></script>
    //<script src="assets/scripts/dotjem/directives/dxSiteFooter.js"></script>
    //<script src="assets/scripts/dotjem/directives/dxSiteHeader.js"></script>
}