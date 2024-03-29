﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using DotJEM.AdvParsers;
using DotJEM.Diagnostic;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Migration;
using DotJEM.Pipelines;
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.AsyncPipeline;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using DotJEM.Web.Host.Util;
using DotJEM.Web.Host.Writers;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DotJEM.Web.Host
{
    public interface IWebHost
    {
        IWebHost Start();
        T Resolve<T>();
        void Shutdown();
    }

    public abstract class WebHost : IWebHost
    {
        //TODO: This is a hack for giving the page controller access.
        public static IInitializationTracker Initialization { get; private set; }

        private readonly IWindsorContainer container;
        private readonly HttpConfiguration configuration;
        private IStorageIndexManager indexManager;
        private IStorageManager storageManager;

        protected IStorageIndex Index { get; set; }
        protected IStorageContext Storage { get; set; }
        protected IAppConfigurationProvider AppConfigurationProvider { get; set; }
        protected IWebHostConfiguration Configuration { get; set; }
        protected IDiagnosticsLogger DiagnosticsLogger { get; set; }

        public HttpConfiguration HttpConfiguration => configuration;

        protected WebHost()
            : this(GlobalConfiguration.Configuration, new WindsorContainer())
        {
        }

        protected WebHost(HttpConfiguration configuration)
            : this(configuration, new WindsorContainer())
        {
        }

        protected WebHost(IWindsorContainer container)
            : this(GlobalConfiguration.Configuration, container)
        {
        }

        protected WebHost(HttpConfiguration configuration, IWindsorContainer container)
        {
            Initialization = new InitializationTracker();

            this.configuration = configuration;
            this.container = container;

            configuration.Services.Replace(typeof(IHttpControllerSelector), new ControllerSelector(configuration));
            configuration.Services.Replace(typeof(IHttpControllerActivator), new WindsorControllerActivator(container));

            container.Kernel.Resolver.AddSubResolver(new ArraySubResolver(container.Kernel));

            configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
            configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter());
        }


        public IWebHost Start()
        {
            container.Install(FromAssembly.This());

            BeforeStart();

            AppConfigurationProvider = container.Resolve<IAppConfigurationProvider>();
            Configuration = AppConfigurationProvider.Get<WebHostConfiguration>();

            AttachIndexDebugging();
            Index = CreateIndex();
            Storage = CreateStorage();

            container
                .Register(Component.For<IPathResolver>().ImplementedBy<PathResolver>())
                .Register(Component.For<IJsonMergeVisitor>().ImplementedBy<JsonMergeVisitor>())
                .Register(Component.For<IDiagnosticsDumpService>().ImplementedBy<DiagnosticsDumpService>())
                .Register(Component.For<IJsonConverter>().ImplementedBy<DotjemJsonConverter>())
                .Register(Component.For<ILazyComponentLoader>().ImplementedBy<LazyOfTComponentLoader>())
                .Register(Component.For<IWindsorContainer>().Instance(container))
                .Register(Component.For<IWebHost>().Instance(this))
                .Register(Component.For<IStorageIndex>().Instance(Index))
                .Register(Component.For<IStorageContext>().Instance(Storage))
                .Register(Component.For<IWebHostConfiguration>().Instance(Configuration))
                .Register(Component.For<IInitializationTracker>().Instance(Initialization));

            ILogger perf = container.Resolve<ILogger>();
            IPerformanceTracker startup = perf.TrackTask("Start");

            DiagnosticsLogger = container.Resolve<IDiagnosticsLogger>();

            perf.TrackAction(BeforeConfigure);
            perf.TrackAction(() => Configure(container.Resolve<IPipelines>()), "Configure Pipeline");
            perf.TrackAction(() => Configure(container), "Configure Container");
            perf.TrackAction(() => Configure(Storage), "Configure Storage");
            perf.TrackAction(() => Configure(Index), "Configure Index");
            perf.TrackAction(() => Configure(new HttpRouterConfigurator(configuration.Routes)), "Configure Routes");
            perf.TrackAction(AfterConfigure);

            ResolveComponents();

            Initialization.SetProgress("Bootstrapping.");
            Task.Factory.StartNew(() =>
            {
                perf.TrackAction(BeforeInitialize);
                Initialization.SetProgress("Initializing storage.");
                perf.TrackAction(() => Initialize(Storage), "Initialize Storage");
                Initialization.SetProgress("Initializing index.");
                perf.TrackAction(() => Initialize(Index), "Initialize Index");

                perf.TrackAction(AfterInitialize);

                storageManager = container.Resolve<IStorageManager>();
                indexManager = container.Resolve<IStorageIndexManager>();
                Initialization.SetProgress("Loading index.");

 
                perf.TrackAction(storageManager.Start);
                perf.TrackAction(indexManager.Start);
                perf.TrackAction(AfterStart);

                Initialization.Complete();
                startup.Dispose();
            }).ContinueWith(async result => {
                if (!result.IsFaulted)
                    return;

                IDiagnosticsDumpService dump = Resolve<IDiagnosticsDumpService>();

                Guid ticket = Guid.NewGuid();
                try
                {
                    if (result.Exception != null)
                    {
                        DiagnosticsLogger.LogException(Severity.Fatal, result.Exception, new {ticketId = ticket});
                        dump.Dump(ticket, result.Exception.ToString());
                    }
                    else
                    {
                        DiagnosticsLogger.LogFailure(Severity.Fatal, "Server startup failed. Unknown Error.", new {ticketId = ticket});
                        dump.Dump(ticket, "Server startup failed. Unknown Error.");
                    }

                    Initialization.SetProgress("Server startup failed. Please contact support. ({0})", ticket);
                }
                catch (Exception ex)
                {
                    //TODO: (jmd 2015-10-01) Temporary Dumping of failure we don't know where to put. 
                    string dumpMessage =
                        $"{ex}{Environment.NewLine}-----------------------------------{Environment.NewLine}{result.Exception}";
                    Initialization.SetProgress(dumpMessage);
                    dump.Dump(ticket, dumpMessage);
                }

                await Task.Delay(10.Minutes())
                    .ContinueWith(t =>
                    {
                        //NOTE: (jmd 2019-11-04) This restarts the application.
                        HttpRuntime.UnloadAppDomain();
                    });
            });
            return this;
        }

        private void AttachIndexDebugging()
        {
            IndexDebuggingConfiguration config = Configuration.Index.Debugging;
            if (!config.Enabled) 
                return;

            InfoStreamConfiguration writerConfig = config?.IndexWriterInfoStream;
            if (writerConfig != null)
            {
                string path = HostingEnvironment.MapPath(writerConfig.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                IndexWriter.DefaultInfoStream = new RollingStreamWriter(path, AdvParser.ParseByteCount(writerConfig.MaxSize), writerConfig.MaxFiles, writerConfig.Zip);
            }
        }

        protected virtual void ResolveComponents()
        {
            container.ResolveAll<IExceptionLogger>().ForEach(logger => HttpConfiguration.Services.Add(typeof(IExceptionLogger), logger));
            IExceptionHandler handler = container.Resolve<IExceptionHandler>();
            configuration.Services.Replace(typeof(IExceptionHandler), handler);

            configuration.MessageHandlers.Add(new PerformanceLoggingHandler(container.Resolve<ILogger>()));
            container
                .ResolveAll<IDataMigrator>()
                .ForEach(migrator => Storage.MigrationManager.Add(migrator));
        }


        protected virtual IStorageIndex CreateIndex(Analyzer analyzer = null)
        {
            IndexStorageConfiguration storage = Configuration.Index.Storage;

            if (storage == null)
                return new LuceneStorageIndex();

            switch (storage.Type)
            {
                case IndexStorageType.File:
                    return new LuceneStorageIndex(new LuceneFileIndexStorage(HostingEnvironment.MapPath(storage.Path)), analyzer: analyzer);
                case IndexStorageType.CachedMemory:
                    return new LuceneStorageIndex(new LuceneCachedMemmoryIndexStorage(HostingEnvironment.MapPath(storage.Path)), analyzer: analyzer);
                case IndexStorageType.Memory:
                    return new LuceneStorageIndex(analyzer: analyzer);
                default:
                    return new LuceneStorageIndex(analyzer: analyzer);
            }
        }


        protected virtual IStorageContext CreateStorage()
        {
            var context = new SqlServerStorageContext(Configuration.Storage.ConnectionString);
            return context;
        }

        public T Resolve<T>()
        {
            return container.Resolve<T>();
        }


        protected virtual void BeforeStart() { }
        protected virtual void BeforeConfigure() { }
        protected virtual void Configure(IWindsorContainer container) { }
        protected virtual void Configure(IStorageContext storage) { }
        protected virtual void Configure(IStorageIndex index) { }
        protected virtual void Configure(IRouter router) { }
        protected virtual void Configure(IPipelines pipeline) { }

        protected virtual void AfterConfigure() { }
        protected virtual void BeforeInitialize() { }
        protected virtual void Initialize(IStorageContext storage) { }
        protected virtual void Initialize(IStorageIndex index) { }

        protected virtual void AfterInitialize() { }
        protected virtual void AfterStart() { }

        public void Shutdown()
        {
            Resolve<IWebScheduler>().Stop();

            Index.Close();
        }
    }


}
