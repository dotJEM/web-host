using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Castle.MicroKernel.Resolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using DotJEM.AdvParsers;
using DotJEM.Diagnostic;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Analysis;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Documents;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Tracking;
using DotJEM.Json.Index2.Snapshots;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Migration;
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.DataCleanup;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Initialization;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using DotJEM.Web.Host.Providers.Data.Storage;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services.DiffMerge;
using DotJEM.Web.Host.Tasks;
using DotJEM.Web.Host.Util;
using DotJEM.Web.Host.Writers;
using DotJEM.Web.Scheduler;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Index;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Component = Castle.MicroKernel.Registration.Component;

namespace DotJEM.Web.Host;

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
    private IJsonIndexManager indexManager;
    private IJsonStorageManager storageManager;

    protected IJsonIndex Index { get; set; }
    protected IJsonIndexManager IndexManager { get; set; }
    protected IStorageContext Storage { get; set; }
    protected IJsonStorageManager StorageManager { get; set; }
    protected IWebTaskScheduler Scheduler { get; set; }
    protected IAppConfigurationProvider AppConfigurationProvider { get; set; }
    protected IWebHostConfiguration Configuration { get; set; }
    protected IDiagnosticsLogger DiagnosticsLogger { get; set; }
    protected ISchemaCollection Schemas { get; private set; }

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
        IPathResolver path = new PathResolver();

        AppConfigurationProvider = container.Resolve<IAppConfigurationProvider>();
        Configuration = AppConfigurationProvider.Get<WebHostConfiguration>();
        SetupKillSignal(path);

        IQueryParserConfiguration parserConfiguration = new QueryParserConfiguration();
        Schemas = new SchemaCollection();
        Index = BuildIndex(
            Schemas,
            parserConfiguration,
            config => new JsonAnalyzer(config.Version),
            new JsonIndexBuilder("Main")
            ).Build();
        Storage = CreateStorage();
        Scheduler = CreateScheduler();

        container
            .Register(Component.For<IPathResolver>().Instance(path))
            .Register(Component.For<ISchemaCollection>().Instance(Schemas))
            //.Register(Component.For<IWebTaskScheduler>().Instance(Scheduler))
            .Register(Component.For<IJsonMergeVisitor>().ImplementedBy<JsonMergeVisitor>())
            .Register(Component.For<IDiagnosticsDumpService>().ImplementedBy<DiagnosticsDumpService>())
            .Register(Component.For<IJsonConverter>().ImplementedBy<DotjemJsonConverter>())
            .Register(Component.For<ILazyComponentLoader>().ImplementedBy<LazyOfTComponentLoader>())
            .Register(Component.For<IWindsorContainer>().Instance(container))
            .Register(Component.For<IWebHost>().Instance(this))
            .Register(Component.For<IJsonIndex>().Instance(Index))
            .Register(Component.For<IStorageContext>().Instance(Storage))
            .Register(Component.For<IWebHostConfiguration>().Instance(Configuration))
            .Register(Component.For<IInitializationTracker>().Instance(Initialization));

        ILogger perf = container.Resolve<ILogger>();
        IPerformanceTracker startup = perf.TrackTask("Start");

        DiagnosticsLogger = container.Resolve<IDiagnosticsLogger>();

        perf.TrackAction(BeforeConfigure);
        perf.TrackAction(() => Configure(parserConfiguration), "Configure Query Parser");
        perf.TrackAction(() => Configure(container.Resolve<IPipeline>()), "Configure Pipeline");
        perf.TrackAction(() => Configure(container), "Configure Container");
        perf.TrackAction(() => Configure(Storage), "Configure Storage");
        //perf.TrackAction(() => Configure(Index), "Configure Index");
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

            storageManager = container.Resolve<IJsonStorageManager>();
            indexManager = container.Resolve<IJsonIndexManager>();
            Initialization.SetProgress("Loading index.");
            indexManager.InfoStream.Subscribe(Initialization);

            //indexManager.InfoStream.Subscribe(new StorageIndexStartupTracker(Initialization));

            Sync.FireAndForget(indexManager.RunAsync());
            perf.TrackTask(indexManager.Tracker.WhenState(IngestInitializationState.Initialized), "Index Manager");
            perf.TrackAction(storageManager.Start);
            container.Resolve<IDataCleanupManager>().Start();

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

            await Task.Delay(5.Minutes())
                .ContinueWith(t =>
                {
                    //NOTE: (jmd 2019-11-04) This restarts the application.
                    HttpRuntime.UnloadAppDomain();
                });
        });
        return this;
    }


    protected virtual void SetupKillSignal(IPathResolver path)
    {
        if(string.IsNullOrEmpty(Configuration.KillSignalFile))
            return;

        string killFile = path.MapPath(Configuration.KillSignalFile);
        string dir = Path.GetDirectoryName(killFile);

        byte[] time = BitConverter.GetBytes(DateTime.Now.Ticks);
        byte[] owner = BitConverter.GetBytes(Process.GetCurrentProcess().Id);
        byte[] signature = time.Concat(owner).ToArray();

        Directory.CreateDirectory(dir);
        File.WriteAllBytes(killFile, signature);

        FileSystemWatcher watcher =  new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(killFile);
        watcher.Filter = "kill.signal";
        watcher.Changed += (sender, args) =>
        {
            byte[] content = File.ReadAllBytes(killFile);
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] != signature[i]) Process.GetCurrentProcess().Kill();
            }
        };
        watcher.EnableRaisingEvents = true;
    }


    protected virtual void ResolveComponents()
    {
        container.ResolveAll<IExceptionLogger>().ForEach(logger => HttpConfiguration.Services.Add(typeof(IExceptionLogger), logger));
        IExceptionHandler handler = container.Resolve<IExceptionHandler>();
        configuration.Services.Replace(typeof(IExceptionHandler), handler);

        configuration.MessageHandlers.Add(new PerformanceLoggingHandler(container.Resolve<ILogger>()));
        container
            .ResolveAll<IDataMigrator>()
            .ForEach(Storage.MigrationManager.Add);
    }

    protected virtual IWebTaskScheduler CreateScheduler() => new WebTaskScheduler();

    protected virtual IJsonIndexBuilder BuildIndex(
        ISchemaCollection schemas, IQueryParserConfiguration config,
        Func<IJsonIndexConfiguration,Analyzer> analyzerProvider = null,
        IJsonIndexBuilder builder = null)
    {
        IndexConfiguration configuration = Configuration.Index;

        analyzerProvider ??= c => new JsonAnalyzer(c.Version); 
        builder ??= new JsonIndexBuilder("Main");

        builder.WithClassicLuceneQueryParser(schemas, config);
        builder.WithAnalyzer(analyzerProvider);
        builder.TryWithService<ILuceneDocumentFactory>(
            c => new WebHostLuceneDocumentFactory(new LuceneDocumentFactory(c.FieldInformationManager), schemas));
        if (configuration.Storage == null)
        {
            return builder
                .UsingMemmoryStorage()
                .WithSnapshoting();
        }

        if (configuration.Snapshots != null)
        {
            builder.WithSnapshoting();
        }

        builder = configuration.Storage.Type switch
        {
            IndexStorageType.Memory => builder.UsingMemmoryStorage(),
            IndexStorageType.File => builder.UsingSimpleFileStorage(HostingEnvironment.MapPath(configuration.Storage.Path)),
            IndexStorageType.CachedMemory => builder.UsingSimpleFileStorage(HostingEnvironment.MapPath(configuration.Storage.Path)),
            _ => throw new ArgumentOutOfRangeException()
        };

        return builder;
    }
    
    protected virtual IStorageContext CreateStorage() => new SqlServerStorageContext(Configuration.Storage.ConnectionString);



  
    public T Resolve<T>() => container.Resolve<T>();


    protected virtual void BeforeStart() { }
    protected virtual void BeforeConfigure() { }
    protected virtual void Configure(IWindsorContainer container) { }
    protected virtual void Configure(IStorageContext storage) { }
    protected virtual void Configure(IRouter router) { }
    protected virtual void Configure(IPipeline pipeline) { }
    protected virtual void Configure(IQueryParserConfiguration parserConfig) { }

    protected virtual void AfterConfigure() { }
    protected virtual void BeforeInitialize() { }
    protected virtual void Initialize(IStorageContext storage) { }
    protected virtual void Initialize(IJsonIndex index) { }

    protected virtual void AfterInitialize() { }
    protected virtual void AfterStart() { }

    public void Shutdown()
    {
        Resolve<IWebTaskScheduler>().Stop();
        Index.Close();
    }
}