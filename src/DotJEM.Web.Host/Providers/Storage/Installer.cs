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
using static Lucene.Net.Documents.Field;
using System.Reactive.Concurrency;

namespace DotJEM.Web.Host.Providers.Storage;

public class Installer : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IJsonStorageManager>().ImplementedBy<JsonStorageManager>().LifestyleSingleton());
        container.Register(Component.For<IStorageChangeFilterHandler>().ImplementedBy<StorageChangeFilterHandler>().LifestyleSingleton());


        container.Register(Component.For<ISnapshotStrategy>().UsingFactoryMethod(kernel =>
        {
            IPathResolver path = kernel.Resolve<IPathResolver>();
            IWebHostConfiguration configuration = kernel.Resolve<IWebHostConfiguration>();
            return new ZipSnapshotStrategy(path.MapPath(configuration.Index.Snapshots.Path), configuration.Index.Snapshots.MaxSnapshots);

        }).LifestyleSingleton());

        container.Register(Component.For<IJsonIndexSnapshotManager>().UsingFactoryMethod(kernel =>
        {
            ISnapshotStrategy snapshotStrategy = kernel.Resolve<ISnapshotStrategy>();
            IWebTaskScheduler scheduler = kernel.Resolve<IWebTaskScheduler>();
            IJsonIndex index = kernel.Resolve<IJsonIndex>();
            IWebHostConfiguration configuration = kernel.Resolve<IWebHostConfiguration>();
            return new JsonIndexSnapshotManager(index, snapshotStrategy, scheduler, configuration.Index.Snapshots.Interval);
        }).LifestyleSingleton());
        container.Register(Component.For<IJsonIndexManager>().UsingFactoryMethod(kernel =>
        {
            IJsonIndex index = kernel.Resolve<IJsonIndex>();
            IWebTaskScheduler scheduler = kernel.Resolve<IWebTaskScheduler>();
            IJsonStorageManager storageManager = kernel.Resolve<IJsonStorageManager>();
            IJsonIndexSnapshotManager snapshotsManager = kernel.Resolve<IJsonIndexSnapshotManager>();
            return new JsonIndexManager(new JsonStorageDocumentSource(storageManager.Observers), snapshotsManager, new JsonIndexWriter(index, scheduler)
            );
        }).LifestyleSingleton());

    }
}