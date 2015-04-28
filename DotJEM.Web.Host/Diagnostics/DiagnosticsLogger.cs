using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics
{
    public interface IDiagnosticsLogger
    {
        JObject Log(string contentType, Severity severity, JObject entity);
        JObject Log(string contentType, Severity severity, string message, JObject entity = null);

        JObject LogIncident(Severity severity, JObject entity);
        JObject LogIncident(Severity severity, string message, JObject entity = null);
        JObject LogWarning(Severity severity, JObject entity);
        JObject LogWarning(Severity severity, string message, JObject entity = null);
        JObject LogFailure(Severity severity, JObject entity);
        JObject LogFailure(Severity severity, string message, JObject entity = null);
    }

    public class DiagnosticsLoggerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IDiagnosticsLogger>().ImplementedBy<DiagnosticsLogger>().LifestyleTransient());
        }
    }

    public enum Severity
    {
        Debug,
        Verbose,
        Trace,
        Message,
        Information,
        Status,
        Warning,
        Error,
        Critical,
        Fatal
    }

    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public const string ContentTypeIncident = "incident";
        public const string ContentTypeWarning = "warning";
        public const string ContentTypeFailure = "failure";

        private readonly IStorageArea area;
        private readonly IStorageIndexManager manager;

        public DiagnosticsLogger(IStorageContext context, IStorageIndexManager manager)
        {
            this.area = context.Area("diagnostic");
            this.manager = manager;
        }

        public JObject Log(string contentType, Severity severity, JObject entity)
        {
            entity["severity"] = JToken.FromObject(severity);
            entity = area.Insert(contentType, entity);
            manager.QueueUpdate(entity);
            return entity;
        }

        public JObject Log(string contentType, Severity severity, string message, JObject entity = null)
        {
            if (entity == null)
                entity = new JObject();

            entity["message"] = message;
            return Log(contentType, severity, entity);
        }

        public JObject LogIncident(Severity severity, JObject entity)
        {
            return Log(ContentTypeIncident, severity, entity);
        }

        public JObject LogIncident(Severity severity, string message, JObject entity = null)
        {
            
            return Log(ContentTypeIncident, severity, message, entity);
        }

        public JObject LogWarning(Severity severity, JObject entity)
        {
            return Log(ContentTypeWarning, severity, entity);
        }

        public JObject LogWarning(Severity severity, string message, JObject entity = null)
        {
            return Log(ContentTypeWarning, severity, message, entity);
        }

        public JObject LogFailure(Severity severity, JObject entity)
        {
            return Log(ContentTypeFailure, severity, entity);
        }

        public JObject LogFailure(Severity severity, string message, JObject entity = null)
        {
            return Log(ContentTypeFailure, severity, message, entity);
        }
    }
}