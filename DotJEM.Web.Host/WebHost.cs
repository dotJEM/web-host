using System.Web.Http;
using System.Web.Http.Dispatcher;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Castle;

namespace DotJEM.Web.Host
{
    public interface IWebHost
    {
        IWebHost Start();
    }

    public abstract class WebHost : IWebHost
    {
        private readonly IWindsorContainer container;
        private readonly HttpConfiguration configuration;

        protected IStorageIndex Index { get; set; }
        protected IStorageContext Storage { get; set; }

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
        }


        public IWebHost Start()
        {
            container.Install(FromAssembly.This());

            Index = CreateIndex();
            Storage = CreateStorage();

            container
                .Register(Component.For<IStorageIndex>().Instance(Index))
                .Register(Component.For<IStorageContext>().Instance(Storage));

            BeforeConfigure();

            var routing = new HttpRouting<HttpConfiguration>(configuration);

            Configure(container);
            Configure(Storage);
            Configure(Index);
            Configure(routing);

            AfterConfigure();
            BeforeInitialize();

            Initialize(Storage);
            Initialize(Index);

            AfterInitialize();

            return this;
        }

        protected virtual void BeforeStart() { }

        protected virtual IStorageIndex CreateIndex() { return new LuceneStorageIndex(); }
        protected virtual IStorageContext CreateStorage()
        {
            //TODO: use app.config (web.config)
            return new SqlServerStorageContext("Data Source=.\\DEV;Initial Catalog=dotjem3;Integrated Security=True");
        }

        protected virtual void BeforeConfigure() { }
        protected virtual void Configure(IWindsorContainer container) { }
        protected virtual void Configure(IStorageContext storage) { }
        protected virtual void Configure(IStorageIndex index) { }
        protected virtual void Configure(IRouting routing) { }
        protected virtual void AfterConfigure() { }

        protected virtual void BeforeInitialize() { }
        protected virtual void Initialize(IStorageContext storage) { }
        protected virtual void Initialize(IStorageIndex index) { }
        protected virtual void AfterInitialize() { }

        protected virtual void AfterStart() { }
    }
}