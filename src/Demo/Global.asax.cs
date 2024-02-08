using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Demo.Controllers;
using DotJEM.Json.Index2;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Documents.Fields;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Configuration;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Providers.Data;
using DotJEM.Web.Host.Providers.Data.Index;
using DotJEM.Web.Host.Providers.Data.Index.Builder;
using DotJEM.Web.Host.Providers.Data.Index.Schemas;
using DotJEM.Web.Host.Providers.Data.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Search;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            new DemoHost(GlobalConfiguration.Configuration).Start();
        }
    }
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

        protected override IJsonIndexBuilder BuildIndex(ISchemaCollection schemas, IQueryParserConfiguration config, Func<IJsonIndexConfiguration, Analyzer> analyzerProvider = null)
        {
            return base.BuildIndex(schemas, config, analyzerProvider)
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

            //storage.Configure.VersionProvider = version;

            base.Configure(storage);
        }

        protected override void Configure(IQueryParserConfiguration parserConfig)
        {
            BooleanQuery.MaxClauseCount = 65535;
            //index.Configuration.SetTypeResolver("contentType");
            //index.Configuration.SetRawField("$raw");
            //index.Configuration.SetScoreField("$score");
            //index.Configuration.SetIdentity("id");

            parserConfig.Field("users.identifier", new TermFieldStrategy());
            parserConfig.Field("groups.identifier", new TermFieldStrategy());

            parserConfig.Field("updatedBy.user", new TermFieldStrategy());
            parserConfig.Field("createdBy.user", new TermFieldStrategy());

            parserConfig.Field("owner", new TermFieldStrategy());
            parserConfig.Field("imo", new TermFieldStrategy());

            //index.Configuration.ForAll().Index("users.identifier", As.Term);
            //index.Configuration.ForAll().Index("groups.identifier", As.Term);

            //index.Configuration.ForAll().Index("updatedBy.user", As.Term);
            //index.Configuration.ForAll().Index("createdBy.user", As.Term);

            //index.Configuration.ForAll().Index("owner", As.Term);

            //index.Configuration.For("ship").Index("imo", As.Term);

            //index.Configuration.SetSerializer(new ZipJsonDocumentSerializer());
        }

        protected override void AfterInitialize()
        {
            Resolve<IDataStorageManager>().InfoStream.Subscribe(WriteDebug);
        }

        private void WriteDebug(IInfoStreamEvent evt)
        {
            Debug.WriteLine(evt);
        }
    }
}
