using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.Ext
{
    public static class DiagnosticsLoggerIncidentExtensions
    {
        private const string ContentType = "incident";

        public static void LogIncident(this IDiagnosticsLogger self, Severity severity, object entity)
        {
            self.Log(ContentType, severity, entity);
        }

        public static void LogIncident(this IDiagnosticsLogger self, Severity severity, string message, object entity = null)
        {
            self.Log(ContentType, severity, message, entity);
        }

        public static void LogIncident(this IDiagnosticsLogger self, Severity severity, string message, Exception exception, object entity = null)
        {
            JObject json = entity != null ? (entity as JObject ?? self.Converter.ToJObject(entity)) : new JObject();
            json["exception"] = self.Converter.ToJObject(exception);
            self.Log(ContentType, severity, message, json);
        }
    }
}