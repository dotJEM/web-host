using System;
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
using DotJEM.Web.Host;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Diagnostics.ExceptionHandlers;
using DotJEM.Web.Host.Providers.Index;
using DotJEM.Web.Host.Providers.Index.Builder;
using DotJEM.Web.Host.Providers.Index.Schemas;
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
    }
}
