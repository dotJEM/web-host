using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Migration;
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Util;
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

        protected IStorageIndex Index { get; set; }
        protected IStorageContext Storage { get; set; }
        protected IAppConfigurationProvider AppConfigurationProvider { get; set; }
        protected IWebHostConfiguration Configuration { get; set; }
        protected IDiagnosticsLogger DiagnosticsLogger { get; set; }

        public HttpConfiguration HttpConfiguration
        {
            get { return configuration; }
        }

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

            configuration.Services.Replace(typeof (IHttpControllerSelector), new ControllerSelector(configuration));
            configuration.Services.Replace(typeof (IHttpControllerActivator), new WindsorControllerActivator(container));

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

            Index = CreateIndex();
            Storage = CreateStorage();

            container
                .Register(Component.For<IPathResolver>().ImplementedBy<PathResolver>())
                .Register(Component.For<IJsonConverter>().ImplementedBy<DotjemJsonConverter>())
                .Register(Component.For<ILazyComponentLoader>().ImplementedBy<LazyOfTComponentLoader>())
                .Register(Component.For<IWindsorContainer>().Instance(container))
                .Register(Component.For<IWebHost>().Instance(this))
                .Register(Component.For<IStorageIndex>().Instance(Index))
                .Register(Component.For<IStorageContext>().Instance(Storage))
                .Register(Component.For<IWebHostConfiguration>().Instance(Configuration))
                .Register(Component.For<IInitializationTracker>().Instance(Initialization));

            var perf = container.Resolve<IPerformanceLogger>();
            var startup = perf.TrackTask("Start");

            DiagnosticsLogger = container.Resolve<IDiagnosticsLogger>();
          
            perf.TrackAction(BeforeConfigure);
            perf.TrackAction("Configure Pipeline", () => Configure(container.Resolve<IPipeline>()));
            perf.TrackAction("Configure Container", () => Configure(container));
            perf.TrackAction("Configure Storage", () => Configure(Storage));
            perf.TrackAction("Configure Index", () => Configure(Index));
            perf.TrackAction("Configure Routes", () => Configure(new HttpRouterConfigurator(configuration.Routes)));
            perf.TrackAction(AfterConfigure);

            ResolveComponents();
            
            Initialization.SetProgress("Bootstrapping.");
            Task.Factory.StartNew(() =>
            {
                perf.TrackAction(BeforeInitialize);
                Initialization.SetProgress("Initializing storage.");
                perf.TrackAction("Initialize Storage", () => Initialize(Storage));
                Initialization.SetProgress("Initializing index.");
                perf.TrackAction("Initialize Index", () => Initialize(Index));

                perf.TrackAction(AfterInitialize);

                indexManager = container.Resolve<IStorageIndexManager>();
                Initialization.SetProgress("Loading index.");
                perf.TrackAction(indexManager.Start);
                perf.TrackAction(AfterStart);

                startup.Trace("");
                Initialization.Complete();
                
            }).ContinueWith(result =>
            {
                if (!result.IsFaulted) 
                    return;
                Guid ticket = Guid.NewGuid();
                if (result.Exception != null)
                {
                    DiagnosticsLogger.LogException(Severity.Fatal, result.Exception, new { ticketId = ticket });
                }
                else
                {
                    DiagnosticsLogger.LogFailure(Severity.Fatal, "Server startup failed. Unknown Error.", new { ticketId = ticket });
                }
                Initialization.SetProgress("Server startup failed. Please contact support. ({0})", ticket);
            });
            return this;
        }

        private void ResolveComponents()
        {
            container.ResolveAll<IExceptionLogger>()
                .ForEach(logger => HttpConfiguration.Services.Add(typeof (IExceptionLogger), logger));
            configuration.Services.Replace(typeof (IExceptionHandler), container.Resolve<IExceptionHandler>());

            configuration.MessageHandlers.Add(new PerformanceLoggingHandler(container.Resolve<IPerformanceLogger>()));
            container
                .ResolveAll<IDataMigrator>()
                .ForEach(migrator => Storage.Migrators.Add(migrator));
        }


        protected virtual IStorageIndex CreateIndex()
        {
            string cachePath = Configuration.Index.CacheLocation;
            if (!string.IsNullOrEmpty(cachePath))
            {
                cachePath = HostingEnvironment.MapPath(cachePath);
                return new LuceneStorageIndex(new LuceneCachedMemmoryIndexStorage(cachePath));
            }
            //TODO: use app.config (web.config)
            return new LuceneStorageIndex();
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
        protected virtual void Configure(IPipeline pipeline) { }
        protected virtual void AfterConfigure() { }
        protected virtual void BeforeInitialize() { }
        protected virtual void Initialize(IStorageContext storage) { }
        protected virtual void Initialize(IStorageIndex index) { }
        protected virtual void AfterInitialize() { }
        protected virtual void AfterStart() { }

        public void Shutdown()
        {
            Index.Close();
        }
    }

    public interface IPathResolver
    {
        string MapPath(string path);
    }

    public class PathResolver : IPathResolver
    {
        public string MapPath(string path)
        {
            return HostingEnvironment.MapPath(path);
        }
    }
}