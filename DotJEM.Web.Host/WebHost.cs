using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Providers;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Providers.Pipeline;
using DotJEM.Web.Host.Providers.Services;
using DotJEM.Web.Host.Util;
using Lucene.Net.Search.Vectorhighlight;
using Newtonsoft.Json;
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
        private readonly IWindsorContainer container;
        private readonly HttpConfiguration configuration;
        private IStorageIndexManager indexManager;

        protected IStorageIndex Index { get; set; }
        protected IStorageContext Storage { get; set; }
        protected IAppConfigurationProvider AppConfigurationProvider { get; set; }
        protected IWebHostConfiguration Configuration { get; set; }

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
            this.configuration = configuration;
            this.container = container;

            configuration.Services.Replace(typeof(IHttpControllerSelector), new ControllerSelector(configuration));
            configuration.Services.Replace(typeof(IHttpControllerActivator), new WindsorControllerActivator(container));
            
            configuration.MessageHandlers.Add(new DiagnosticsLoggingHandler());

            container.Kernel.Resolver.AddSubResolver(new ArraySubResolver(container.Kernel));
            
            configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
        }

        public IWebHost Start()
        {
            BeforeStart();

            container.Install(FromAssembly.This());

            AppConfigurationProvider = container.Resolve<IAppConfigurationProvider>();
            Configuration = AppConfigurationProvider.Get<WebHostConfiguration>();
            
            Index = CreateIndex();
            Storage = CreateStorage();

            container
                .Register(Component.For<IJsonConverter>().ImplementedBy<DotjemJsonConverter>())
                .Register(Component.For<ILazyComponentLoader>().ImplementedBy<LazyOfTComponentLoader>())
                .Register(Component.For<IWindsorContainer>().Instance(container))
                .Register(Component.For<IWebHost>().Instance(this))
                .Register(Component.For<IStorageIndex>().Instance(Index))
                .Register(Component.For<IStorageContext>().Instance(Storage))
                .Register(Component.For<IWebHostConfiguration>().Instance(Configuration));

            BeforeConfigure();

            Configure(container.Resolve<IPipeline>());
            Configure(container);
            Configure(Storage);
            Configure(Index);
            Configure(new HttpRouterConfigurator(configuration.Routes));

            AfterConfigure();
            BeforeInitialize();

            Initialize(Storage);
            Initialize(Index);

            container.ResolveAll<IExceptionLogger>()
                .ForEach(logger => HttpConfiguration.Services.Add(typeof(IExceptionLogger), logger));
            configuration.Services.Replace(typeof(IExceptionHandler), container.Resolve<IExceptionHandler>());

            AfterInitialize();

            indexManager = container.Resolve<IStorageIndexManager>();
            indexManager.Start();

            AfterStart();

            return this;
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
            return new SqlServerStorageContext(Configuration.Storage.ConnectionString);
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

}