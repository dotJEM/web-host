using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Snapshots;
using DotJEM.Json.Index2.Management.Snapshots.Zip;
using DotJEM.Json.Index2.Management.Writer;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Storage.Cutoff;
using DotJEM.Web.Host.Providers.Storage.Indexing;
using DotJEM.Web.Scheduler;

namespace DotJEM.Web.Host.Providers.Storage;

public class Installer : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IJsonStorageManager>().ImplementedBy<JsonStorageManager>().LifestyleSingleton());
        container.Register(Component.For<IStorageChangeFilterHandler>().ImplementedBy<StorageChangeFilterHandler>().LifestyleSingleton());

        container.Register(Component.For<IJsonIndexManager>().UsingFactoryMethod(kernel =>
        {
            IJsonIndex index = kernel.Resolve<IJsonIndex>();
            IPathResolver path = kernel.Resolve<IPathResolver>();
            IWebTaskScheduler scheduler = kernel.Resolve<IWebTaskScheduler>();
            IWebHostConfiguration configuration = kernel.Resolve<IWebHostConfiguration>();
            IJsonStorageManager storageManager = kernel.Resolve<IJsonStorageManager>();
            JsonIndexManager indexManager = new JsonIndexManager(new JsonStorageDocumentSource(storageManager.Observers),
                new JsonIndexSnapshotManager(index, 
                    new ZipSnapshotStrategy(path.MapPath(configuration.Index.Snapshots.Path), 
                        configuration.Index.Snapshots.MaxSnapshots),
                    scheduler, configuration.Index.Snapshots.CronTime),
                new ManagerJsonIndexWriter(index, scheduler)

            );
            return indexManager;
        }));

    }
}