﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using DotJEM.Reflection.Descriptors;
using DotJEM.Reflection.Descriptors.Descriptors;
using DotJEM.Reflection.Descriptors.Inspection;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Pipeline
{
    public interface IPipelineManager
    {
        IPipeline Lookup(string contentType);
    }

    public class PipelineManager : IPipelineManager
    {
        private readonly IAddonManager addons;
        private readonly List<IJsonDecorator> steps = new List<IJsonDecorator>();
        private readonly Dictionary<string, IPipeline> pipelines = new Dictionary<string, IPipeline>();

        public PipelineManager(IAddonManager addons)
        {
            this.addons = addons;
        }

        public IPipeline Lookup(string contentType)
        {
            return !pipelines.ContainsKey(contentType) ? InitializePipeline(contentType) : pipelines[contentType];
        }

        private IPipeline InitializePipeline(string contentType)
        {
            return pipelines[contentType] = new Pipline(steps.Where(step => step.Accepts(contentType)).ToList());
        }

        public void Invalidate(string contentType = null)
        {
            if (contentType == null)
            {
                pipelines.Clear();
            }
            else
            {
                pipelines.Remove(contentType);
            }
        }
    }

    public interface IAddonManager
    {
        event EventHandler<AddonLoadedEventArgs> AddonLoaded;

        void Initialize();
    }

    public interface IAddonDescriptor
    {
    }

    public class AddonLoadedEventArgs : EventArgs
    {
    }

    public class AddonManager : IAddonManager
    {
        private readonly string root;
        private readonly IDictionary<string, IAddonDescriptor> addons = new Dictionary<string, IAddonDescriptor>();

        public event EventHandler<AddonLoadedEventArgs> AddonLoaded;

        public AddonManager(string root)
        {
            this.root = root;
        }


        public void Initialize()
        {
            DependencyResolver resolver = DependencyResolver.Instance;
            using (IAssemblyInspectionContext context = new AssemblyInspectionContext())
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    resolver.AddLocation(dir);
                    LoadAddons(context, dir);
                }
            }
        }

        private void LoadAddons(IAssemblyInspectionContext context, string directory)
        {
            AssemblyDescriptor descriptor = context.LoadAssembly(GetPrimaryAssembly(directory));

            TypeDescriptor[] decorators = FindTypesImplementing<IJsonDecorator>(descriptor);



            //        FileInfo file = GetPrimaryAssembly(directory);

            //        IAssemblyDescriptor descriptor = inspector.Inspect(file, context.RootDirectory);
            //        IApplicationDescriptor[] applications = LoadApplications(descriptor);
            //        return new AddinDescriptor(descriptor, applications);
            //context.LoadAssembly()
        }

        private TypeDescriptor[] FindTypesImplementing<T>(AssemblyDescriptor descriptor)
        {
            return descriptor.Types.Where(typeDescriptor => 
                (from interfaceType in typeDescriptor.Interfaces
                 where interfaceType.FullName == typeof(T).FullName
                 select interfaceType).Any()).ToArray();
        }

        private static string GetPrimaryAssembly(string directory)
        {
            string file = Directory.GetFiles(directory, "*.manifest").FirstOrDefault();
            if (file != null)
            {
                string content = File.ReadAllText(file);
                //TODO: Simple Manifest implementation, change to proper deserialization implementation.
                string assembly = content;
                if (string.IsNullOrEmpty(assembly))
                    return assembly;
            }
            return Directory.GetFiles(directory, "*.dll").First();
        }

        protected virtual void OnAddonLoaded(AddonLoadedEventArgs e)
        {
            if (AddonLoaded != null) AddonLoaded(this, e);
        }
    }


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

    public interface IPipeline
    {
        JObject Execute(JObject json);
    }

    public class Pipline : IPipeline
    {
        private readonly IEnumerable<IJsonDecorator> steps;

        public Pipline(IEnumerable<IJsonDecorator> steps)
        {
            this.steps = steps;
        }

        public JObject Execute(JObject json)
        {
            return steps.Aggregate(json, (jo, step) => step.Execute(jo));
        }
    }

    public interface IJsonDecorator
    {
        bool Accepts(string contentType);

        JObject Execute(JObject json);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JsonDecoratorAttribute : Attribute
    {
        public string[] ContentTypes { get; private set; }

        public JsonDecoratorAttribute(params string[] contentTypes)
        {
            ContentTypes = contentTypes;
        }
    }

    public abstract class JsonDecorator : IJsonDecorator
    {
        private readonly Lazy<HashSet<string>> accepts;

        protected JsonDecorator()
        {
            accepts = new Lazy<HashSet<string>>(Initialize);
        }

        public virtual bool Accepts(string contentType)
        {
            return accepts.Value.Contains(contentType);
        }

        private HashSet<string> Initialize()
        {
            JsonDecoratorAttribute attr = (JsonDecoratorAttribute)GetType()
                .GetCustomAttributes(typeof(JsonDecoratorAttribute), false)
                .FirstOrDefault();
            return attr != null ? new HashSet<string>(attr.ContentTypes) : new HashSet<string>();
        }

        public abstract JObject Execute(JObject json);
    }

    public abstract class AbstractJsonDecorator : JsonDecorator
    {
        public override JObject Execute(JObject json)
        {
            return OnExecute(json);
        }

        public abstract JObject OnExecute(JObject json);
    }

    public abstract class DinamicJsonDecorator : JsonDecorator
    {
        public override JObject Execute(JObject json)
        {
            return OnExecute(json);
        }

        public abstract dynamic OnExecute(dynamic json);
    }
}