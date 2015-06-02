using System;
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

        JObject LogException(Exception exception);

        void LogIncident(Severity severity, string message, object entity);
        void LogWarning(Severity severity, string message, object entity);
        void LogFailure(Severity severity, string message, object entity);
    }

    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public const string ContentTypeIncident = "incident";
        public const string ContentTypeWarning = "warning";
        public const string ContentTypeFailure = "failure";

        private readonly Lazy<IStorageArea> area;
        private readonly Lazy<IStorageIndexManager> manager;
        private readonly IJsonConverter converter;

        public IStorageArea Area { get { return area.Value; } }
        public IStorageIndexManager Manager { get { return manager.Value; } }

        public DiagnosticsLogger(Lazy<IStorageContext> context, Lazy<IStorageIndexManager> manager, IJsonConverter converter)
        {
            this.area = new Lazy<IStorageArea>(() => context.Value.Area("diagnostic"));
            this.manager = manager;
            this.converter = converter;
        }

        public JObject Log(string contentType, Severity severity, JObject entity)
        {
            entity["severity"] = converter.FromObject(severity);
            entity = Area.Insert(contentType, entity);
            Manager.QueueUpdate(entity);
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

        public JObject LogException(Exception exception)
        {
            return LogFailure(Severity.Error, converter.ToJObject(exception));
        }

        public void LogIncident(Severity severity, string message, object entity)
        {
            LogIncident(severity, message, EnsureJson(entity));
        }

        public void LogWarning(Severity severity, string message, object entity)
        {
            LogWarning(severity, message, EnsureJson(entity));
        }

        public void LogFailure(Severity severity, string message, object entity)
        {
            LogFailure(severity, message, EnsureJson(entity));
        }

        private JObject EnsureJson(object entity)
        {
            return entity as JObject ?? converter.ToJObject(entity);
        }
    }
}