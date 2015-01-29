using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Castle;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Providers;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DotJEM.Web.Host
{
    public interface IWebHost
    {
        IWebHost Start();
        void Shutdown();
    }

    public abstract class WebHost : IWebHost
    {
        private readonly IWindsorContainer container;
        private readonly HttpConfiguration configuration;

        protected IStorageIndex Index { get; set; }
        protected IStorageContext Storage { get; set; }
        protected IAppConfigurationProvider AppConfigurationProvider { get; set; }
        protected IWebHostConfiguration Configuration { get; set; }

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
            
            container.Kernel.Resolver.AddSubResolver(new ArraySubResolver(container.Kernel));

            configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
        }
        
        public IWebHost Start()
        {
            container.Install(FromAssembly.This());

            AppConfigurationProvider = container.Resolve<IAppConfigurationProvider>();
            Configuration = AppConfigurationProvider.Get<WebHostConfiguration>();

            Index = CreateIndex();
            Storage = CreateStorage();

            container
                .Register(Component.For<IStorageIndex>().Instance(Index))
                .Register(Component.For<IStorageContext>().Instance(Storage));

            BeforeConfigure();

            Configure(container);
            Configure(Storage);
            Configure(Index);
            Configure(new HttpRouterConfigurator(configuration.Routes));

            AfterConfigure();
            BeforeInitialize();

            Initialize(Storage);
            Initialize(Index);

            AfterInitialize();

            return this;
        }

        protected virtual void BeforeStart() { }

        protected virtual IStorageIndex CreateIndex()
        {
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

        protected virtual void BeforeConfigure() { }
        protected virtual void Configure(IWindsorContainer container) { }
        protected virtual void Configure(IStorageContext storage) { }
        protected virtual void Configure(IStorageIndex index) { }
        protected virtual void Configure(IRouter router) { }
        protected virtual void AfterConfigure() { }

        protected virtual void BeforeInitialize() { }
        protected virtual void Initialize(IStorageContext storage) { }
        protected virtual void Initialize(IStorageIndex index) { }
        protected virtual void AfterInitialize() { }

        protected virtual void AfterStart() { }

        public void Shutdown()
        {
            
        }
    }
}