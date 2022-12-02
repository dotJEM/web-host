using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Web.Host.Providers.Scheduler;
using DotJEM.Web.Host.Providers.Services;

namespace DotJEM.Web.Host.Providers;

public class ProviderInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IAppConfigurationProvider>().ImplementedBy<AppConfigurationProvider>());
        container.Register(Component.For<IServiceProvider<IContentService>>().ImplementedBy<ContentServiceProvider>());
        container.Register(Component.For<IServiceProvider<ISearchService>>().ImplementedBy<SearchServiceProvider>());
        container.Register(Component.For<IServiceProvider<IFileService>>().ImplementedBy<FileServiceProvider>());

        container.Register(Component.For<IWebScheduler>().ImplementedBy<WebScheduler>());
    }
}