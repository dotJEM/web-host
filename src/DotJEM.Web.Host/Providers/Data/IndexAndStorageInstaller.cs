using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Snapshots;
using DotJEM.Json.Index2.Management.Snapshots.Zip;
using DotJEM.Json.Index2.Management.Source;
using DotJEM.Json.Index2.Management.Writer;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using DotJEM.Web.Host.Providers.Data.Index.Snapshots;
using DotJEM.Web.Host.Providers.Data.Storage;
using DotJEM.Web.Host.Providers.Data.Storage.Cutoff;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Data;

public class IndexAndStorageInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        container.Register(Component.For<IJsonStorageManager>().ImplementedBy<JsonStorageManager>().LifestyleSingleton().IsFallback());
        container.Register(Component.For<IStorageChangeFilterHandler>().ImplementedBy<StorageChangeFilterHandler>().LifestyleSingleton());

        container.Register(Component.For<ISnapshotStrategy>().UsingFactoryMethod(kernel =>
        {
            IPathResolver path = kernel.Resolve<IPathResolver>();
            IWebHostConfiguration configuration = kernel.Resolve<IWebHostConfiguration>();
            return new ZipSnapshotStrategy(path.MapPath(configuration.Index.Snapshots.Path), configuration.Index.Snapshots.MaxSnapshots);

        }).LifestyleSingleton());

        container.Register(Component.For<IJsonIndexSnapshotManager>().UsingFactoryMethod(kernel =>
        {
            IWebHostConfiguration configuration = kernel.Resolve<IWebHostConfiguration>();
            if (configuration.Index.Snapshots == null || configuration.Index.Snapshots.MaxSnapshots ==0)
                return (IJsonIndexSnapshotManager)new NullIndexSnapshotManager();

            ISnapshotStrategy snapshotStrategy = kernel.Resolve<ISnapshotStrategy>();
            IWebTaskScheduler scheduler = kernel.Resolve<IWebTaskScheduler>();
            IJsonIndex index = kernel.Resolve<IJsonIndex>();
            ISchemaCollection schemas = kernel.Resolve<ISchemaCollection>();
            return new WebHostJsonIndexSnapshotManager(index, snapshotStrategy, scheduler, schemas, configuration.Index.Snapshots.Interval);
        }).LifestyleSingleton());

        container.Register(Component.For<IJsonDocumentSource>().UsingFactoryMethod(kernel => kernel.Resolve<IJsonStorageManager>().DocumentSource).LifestyleSingleton());
        container.Register(Component.For<IJsonIndexManager>().ImplementedBy<JsonIndexManager>().LifestyleSingleton());
        container.Register(Component.For<IDataStorageManager>().ImplementedBy<DataStorageManager>().LifestyleSingleton());
    }


}