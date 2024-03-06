using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Documents.Fields;
using DotJEM.Json.Index2.Management.Source;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Configuration;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Providers.Data;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Builder;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using DotJEM.Web.Host.Providers.Data.Storage;
using DotJEM.Web.Scheduler;
using Lucene.Net.Analysis;
using Lucene.Net.Search;

namespace Demo
{
    public class DemoHost : WebHost
    {
        public DemoHost(HttpConfiguration configuration) : base(configuration)
        {
        }

        protected override void Configure(IRouter router)
        {
            router.Route("api/exception").To<ExceptionController>();
            router.Route("api/storage/{area}/{contentType}/{id}")
                .To<StorageController>(x => x.Set.Defaults(new { id = RouteParameter.Optional }));
            router.Route("api/search").To<SearchController>();
            router.Route("api/status").To<StatusController>();
            router.Default("Index").To<IndexController>();
        }

        protected override void Configure(IWindsorContainer container)
        {
            container.Register(Component.For<ExceptionController>().LifestyleTransient());
            container.Register(Component.For<IndexController>().LifestyleTransient());
            container.Register(Component.For<SearchController>().LifestyleTransient());
            container.Register(Component.For<StorageController>().LifestyleTransient());
            container.Register(Component.For<StatusController>().LifestyleTransient());
            container.Register(Component.For<IWebHostExceptionHandler>().ImplementedBy<MyCustomExceptionHandler>());
        }

        protected override IJsonIndexBuilder BuildIndex(ISchemaCollection schemas, IQueryParserConfiguration config, Func<IJsonIndexConfiguration, Analyzer> analyzerProvider = null, IJsonIndexBuilder builder = null)
        {
            return base.BuildIndex(schemas, config, analyzerProvider, builder)
                .WithFieldResolver(new FieldResolver("id", "contentType"));
        }

        
        private static readonly string SchemaVersionFieldName = "$schemaVersion";
        protected override void Configure(IStorageContext storage)
        {
            Resolve<IJsonStorageManager>();
            storage.Configure.MapField(JsonField.Id, "id");
            storage.Configure.MapField(JsonField.ContentType, "contentType");
            storage.Configure.MapField(JsonField.Version, "$version");
            storage.Configure.MapField(JsonField.Created, "$created");
            storage.Configure.MapField(JsonField.Updated, "$updated");
            storage.Configure.MapField(JsonField.SchemaVersion, SchemaVersionFieldName);
            base.Configure(storage);
        }

        protected override void Configure(IQueryParserConfiguration parserConfig)
        {
            BooleanQuery.MaxClauseCount = 65535;
            parserConfig.Field("users.identifier", new TermFieldStrategy());
            parserConfig.Field("groups.identifier", new TermFieldStrategy());

            parserConfig.Field("updatedBy.user", new TermFieldStrategy());
            parserConfig.Field("createdBy.user", new TermFieldStrategy());

            parserConfig.Field("owner", new TermFieldStrategy());
            parserConfig.Field("imo", new TermFieldStrategy());
        }

        protected override void AfterInitialize()
        {
            Resolve<IDataStorageManager>().InfoStream.Subscribe(WriteDebug);
        }

        private void WriteDebug(IInfoStreamEvent evt)
        {
            Debug.WriteLine(evt);
        }
        protected override void AfterConfigure()
        {
            IDiagnosticsLogger logger = Resolve<IDiagnosticsLogger>();
            IDiagnosticsDumpService dump = Resolve<IDiagnosticsDumpService>();
            LogInfoStreamErrorEvents(logger, dump, Resolve<IDataStorageManager>().InfoStream);
            LogInfoStreamErrorEvents(logger, dump, Resolve<IWebTaskScheduler>().InfoStream);
            Resolve<IJsonStorageManager>().DocumentSource.DocumentChanges.Subscribe(change =>
            {
                switch (change)
                {
                    case JsonDocumentCreated created:
                        DebugLog($"Created [{created.Area}]: {created.Document["id"]}");
                        break;
                    case JsonDocumentDeleted deleted:
                        DebugLog($"Deleted [{deleted.Area}]: {deleted.Document["id"]}");
                        break;
                    case JsonDocumentUpdated updated:
                        DebugLog($"Updated [{updated.Area}]: {updated.Document["id"]}");
                        break;
                    case JsonDocumentSourceDigestCompleted completed:
                        DebugLog($"Digest Completed: {completed.Area}");
                        break;
                    case JsonDocumentSourceReset reset:
                        DebugLog($"Source Reset: {reset.Area}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(change));
                }
            });
        }

        private void DebugLog(string message)
        {
            lock (padlock)
            {
                File.AppendAllLines("E:\\TEMP\\templog\\log2.txt", new List<string>() { message });
            }
        }
        private object padlock = new object();
        private void LogInfoStreamErrorEvents(IDiagnosticsLogger logger, IDiagnosticsDumpService dump, IInfoStream infoStream)
        {
            infoStream
                .OfType<InfoStreamExceptionEvent>()
                .Subscribe(evt => {
                    logger.Log("incident", Severity.Error, evt.Message, evt);
                });
#if DEBUG
            infoStream.Subscribe(evt => DebugLog(evt.ToString()));
#endif
        }
    }
}