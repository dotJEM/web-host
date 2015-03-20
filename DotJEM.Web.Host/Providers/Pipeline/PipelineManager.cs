using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class PipelineInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IPipeline>().ImplementedBy<Pipeline>().LifestyleTransient());            
        }
    }

    public interface IPipeline
    {
        JObject ExecuteOnGet(JObject json, string contentType);
        JObject ExecuteOnPut(JObject json, string contentType);
        JObject ExecuteBeforePost(JObject json, string contentType);
        JObject ExecuteAfterPost(JObject json, string contentType);
        JObject ExecuteOnDelete(JObject json, string contentType);
    }

    public interface IJsonDecorator
    {
        bool Accept(string contentType);
        JObject OnGet(dynamic entity);
        JObject OnBeforePost(dynamic entity);
        JObject OnAfterPost(dynamic entity);
        JObject OnPut(dynamic entity);
        JObject OnDelete(dynamic entity);
    }

    public class Pipeline : IPipeline
    {
        private readonly IEnumerable<IJsonDecorator> steps;

        public Pipeline(IJsonDecorator[] steps)
        {
            this.steps = steps;
        }

        public JObject ExecuteOnGet(JObject json, string contentType)
        {
            return steps.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.OnGet(jo));
        }

        public JObject ExecuteOnPut(JObject json, string contentType)
        {
            return steps.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.OnPut(jo));
        }

        public JObject ExecuteBeforePost(JObject json, string contentType)
        {
            return steps.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.OnBeforePost(jo));
        }

        public JObject ExecuteAfterPost(JObject json, string contentType)
        {
            return steps.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.OnAfterPost(jo));
        }

        public JObject ExecuteOnDelete(JObject json, string contentType)
        {
            return steps.Where(step => step.Accept(contentType)).Aggregate(json, (jo, step) => step.OnDelete(jo));
        }

    }

    public abstract class JsonDecorator : IJsonDecorator
    {
        public virtual bool Accept(string contentType)
        {
            return true;
        }

        public virtual JObject OnGet(dynamic entity)
        {
            return entity;
        }

        public virtual JObject OnBeforePost(dynamic entity)
        {
            return entity;
        }

        public virtual JObject OnAfterPost(dynamic entity)
        {
            return entity;
        }

        public virtual JObject OnPut(dynamic entity)
        {
            return entity;
        }

        public virtual JObject OnDelete(dynamic entity)
        {
            return entity;
        }
    }




    //public interface IPipelineManager
    //{
    //    IPipeline Lookup(string contentType);
    //}

    //public class PipelineManager : IPipelineManager
    //{
    //    private readonly IAddonManager addons;
    //    private readonly IKernel kernel;
    //    private readonly IDescriptorResolver resolver = new DescriptorResolver();

    //    private readonly List<IJsonDecorator> steps = new List<IJsonDecorator>();
    //    private readonly Dictionary<string, IPipeline> pipelines = new Dictionary<string, IPipeline>();

    //    public PipelineManager(IAddonManager addons, IKernel kernel)
    //    {
    //        this.addons = addons;
    //        this.kernel = kernel;

    //        addons.AddonLoaded += RegisterDecorators;
    //    }

    //    private void RegisterDecorators(object sender, AddonEventArgs e)
    //    {
    //        steps.AddRange(e.Addon.Decorators.Select(Instantiate));
    //    }

    //    private IJsonDecorator Instantiate(TypeDescriptor descriptor)
    //    {
    //        Type type = resolver.Resolve(descriptor);

    //        var services = (from ctor in type.GetConstructors()
    //                        let parameters = ctor.GetParameters()
    //                        orderby parameters.Length descending
    //                        let parameterTypes = parameters.Select(p => p.ParameterType)
    //                        where parameterTypes.All(kernel.HasComponent)
    //                        select parameterTypes.Select(t => kernel.Resolve(t)))
    //                                       .FirstOrDefault();

    //        return (IJsonDecorator)Activator.CreateInstance(type, services);
    //    }

    //    public IPipeline Lookup(string contentType)
    //    {
    //        return !pipelines.ContainsKey(contentType) ? InitializePipeline(contentType) : pipelines[contentType];
    //    }

    //    private IPipeline InitializePipeline(string contentType)
    //    {
    //        return pipelines[contentType] = new Pipeline(steps.Where(step => step.Accepts(contentType)).ToList());
    //    }

    //    public void Invalidate(string contentType = null)
    //    {
    //        if (contentType == null)
    //        {
    //            pipelines.Clear();
    //        }
    //        else
    //        {
    //            pipelines.Remove(contentType);
    //        }
    //    }
    //}

    //public interface IAddonManager
    //{
    //    event EventHandler<AddonEventArgs> AddonLoaded;

    //    void Initialize();
    //}

    //public interface IAddonDescriptor
    //{
    //    string FullName { get; }
    //    AssemblyDescriptor Assembly { get; }
    //    TypeDescriptor[] Decorators { get; }
    //}

    //public class AddonDescriptor : IAddonDescriptor
    //{
    //    public string FullName { get { return Assembly.FullName; } }

    //    public AssemblyDescriptor Assembly { get; private set; }
    //    public TypeDescriptor[] Decorators { get; private set; }

    //    public AddonDescriptor(AssemblyDescriptor descriptor)
    //    {
    //        Assembly = descriptor;
    //        Decorators = All<IJsonDecorator>(descriptor);
    //    }

    //    private static TypeDescriptor[] All<T>(AssemblyDescriptor descriptor)
    //    {
    //        return descriptor.Types.Where(typeDescriptor =>
    //            (from interfaceType in typeDescriptor.Interfaces
    //             where interfaceType.FullName == typeof(T).FullName
    //             select interfaceType).Any()).ToArray();
    //    }
    //}

    //public class AddonEventArgs : EventArgs
    //{
    //    public IAddonDescriptor Addon { get; private set; }

    //    public AddonEventArgs(IAddonDescriptor addon)
    //    {
    //        Addon = addon;
    //    }
    //}

    //public class AddonManager : IAddonManager
    //{
    //    private readonly string root;
    //    private readonly IDictionary<string, IAddonDescriptor> addons = new Dictionary<string, IAddonDescriptor>();

    //    public event EventHandler<AddonEventArgs> AddonLoaded;
    //    public event EventHandler<AddonEventArgs> AddonUnloaded;

    //    public AddonManager()
    //    {
    //        root = Path.Combine(Directory.GetCurrentDirectory(), "addons");
    //    }

    //    public void Initialize()
    //    {
    //        DependencyResolver resolver = DependencyResolver.Instance;
    //        using (IAssemblyInspectionContext context = new AssemblyInspectionContext())
    //        {
    //            Directory.GetDirectories(root).ForEach(dir => resolver.AddLocation(dir));
    //            EnumerableExtensions.ForEach(Directory.GetDirectories(root)
    //                    .Select(dir => new AddonDescriptor(context.LoadAssembly(GetPrimaryAssembly(dir))))
    //                    .ToArray(), RegisterAddon);
    //        }
    //    }

    //    private void RegisterAddon(IAddonDescriptor addon)
    //    {
    //        if (addons.ContainsKey(addon.FullName))
    //            OnAddonUnloaded(new AddonEventArgs(addons[addon.FullName]));
    //        addons[addon.FullName] = addon;
    //        OnAddonLoaded(new AddonEventArgs(addon));
    //    }

    //    private static string GetPrimaryAssembly(string directory)
    //    {
    //        string file = Directory.GetFiles(directory, "*.manifest").FirstOrDefault();
    //        if (file != null)
    //        {
    //            string content = File.ReadAllText(file);
    //            //TODO: Simple Manifest implementation, change to proper deserialization implementation.
    //            string assembly = content;
    //            if (string.IsNullOrEmpty(assembly))
    //                return assembly;
    //        }
    //        return Directory.GetFiles(directory, "*.dll").First();
    //    }

    //    protected virtual void OnAddonLoaded(AddonEventArgs e)
    //    {
    //        if (AddonLoaded != null) AddonLoaded(this, e);
    //    }

    //    protected virtual void OnAddonUnloaded(AddonEventArgs e)
    //    {
    //        if (AddonUnloaded != null) AddonUnloaded(this, e);
    //    }
    //}


    //internal class ApplicationLoader : IApplicationLoader
    //{
    //    private readonly ICatalog<IAddinDescriptor> repository;
    //    private readonly IServerContext context;
    //    private readonly IAddinInspector inspector;

    //    public ApplicationLoader(IServerContext context, IAddinInspector inspector, ICatalog<IAddinDescriptor> repository)
    //    {
    //        this.inspector = inspector;
    //        this.context = context;
    //        this.repository = repository;
    //    }

    //    public void LoadAddins()
    //    {
    //        DirectoryInfo directory = context.AddinDirectory;
    //        if (!directory.Exists)
    //        {
    //            context.Log.WriteEntry(Level.Info, "Addin Directory '{0}' not found from, please check the configuration.", directory.FullName);
    //            return;
    //        }

    //        context.Log.WriteEntry(Level.Info, "Loading Addins from '{0}'.", directory.FullName);
    //        foreach (DirectoryInfo addinDirectory in directory.GetDirectories())
    //        {
    //            IAddinDescriptor descriptor = inspector.Inspect(addinDirectory);
    //            repository.Add(descriptor);
    //        }
    //    }
    //}

    //internal class AddinInspector : IAddinInspector
    //{
    //    private readonly IAssemblyInspector inspector;
    //    private readonly IServerContext context;

    //    public AddinInspector(IServerContext context, IAssemblyInspector inspector)
    //    {
    //        this.inspector = inspector;
    //        this.context = context;
    //    }

    //    public IAddinDescriptor Inspect(DirectoryInfo directory)
    //    {
    //        context.Log.WriteEntry(Level.Info, "Inspecting Directory '{0}' for addins.", directory.FullName);

    //        FileInfo file = GetPrimaryAssembly(directory);

    //        IAssemblyDescriptor descriptor = inspector.Inspect(file, context.RootDirectory);
    //        IApplicationDescriptor[] applications = LoadApplications(descriptor);
    //        return new AddinDescriptor(descriptor, applications);
    //    }

    //    private IApplicationDescriptor[] LoadApplications(IAssemblyDescriptor descriptor)
    //    {
    //        List<IApplicationDescriptor> tasks = new List<IApplicationDescriptor>();
    //        foreach (ITypeDescriptor typeDescriptor in descriptor.Types)
    //        {
    //            if ((from interfaceType in typeDescriptor.Interfaces
    //                 where interfaceType.FullName == typeof(IApplication).FullName
    //                 select interfaceType).Any())
    //            {
    //                AliasAttribute[] attributes = typeDescriptor.GetCustomAttributes<AliasAttribute>();

    //                context.Log.WriteEntry(Level.Info, "Application '{0}' was found.", typeDescriptor.FullName);
    //                tasks.Add(new ApplicationDescriptor(typeDescriptor, attributes));
    //            }
    //        }
    //        return tasks.ToArray();
    //    }



    //    private static FileInfo GetPrimaryAssembly(DirectoryInfo directory)
    //    {
    //        FileInfo file = directory.GetFiles("manifest").FirstOrDefault();
    //        if (file != null)
    //        {
    //            //TODO: Simple Manifest implementation, change to proper deserialization implementation.
    //            FileInfo assembly = directory.GetFiles(File.ReadAllText(file.FullName)).FirstOrDefault();
    //            if (assembly != null)
    //                return assembly;
    //        }
    //        return directory.GetFiles("*.dll").First();
    //    }
    //}




    //[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    //public class JsonDecoratorAttribute : Attribute
    //{
    //    public string[] ContentTypes { get; private set; }

    //    public JsonDecoratorAttribute(params string[] contentTypes)
    //    {
    //        ContentTypes = contentTypes;
    //    }
    //}

    //public abstract class JsonDecorator : IJsonDecorator
    //{
    //    private readonly Lazy<HashSet<string>> accepts;

    //    protected JsonDecorator()
    //    {
    //        accepts = new Lazy<HashSet<string>>(Initialize);
    //    }

    //    public virtual bool Accepts(string contentType)
    //    {
    //        return accepts.Value.Contains(contentType);
    //    }

    //    private HashSet<string> Initialize()
    //    {
    //        JsonDecoratorAttribute attr = (JsonDecoratorAttribute)GetType()
    //            .GetCustomAttributes(typeof(JsonDecoratorAttribute), false)
    //            .FirstOrDefault();
    //        return attr != null ? new HashSet<string>(attr.ContentTypes) : new HashSet<string>();
    //    }

    //    public abstract JObject Execute(JObject json);
    //}

    //public abstract class AbstractJsonDecorator : JsonDecorator
    //{
    //    public override JObject Execute(JObject json)
    //    {
    //        return OnExecute(json);
    //    }

    //    public abstract JObject OnExecute(JObject json);
    //}

    //public abstract class DinamicJsonDecorator : JsonDecorator
    //{
    //    public override JObject Execute(JObject json)
    //    {
    //        return OnExecute(json);
    //    }

    //    public abstract dynamic OnExecute(dynamic json);
    //}
}