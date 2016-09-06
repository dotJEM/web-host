using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.Ext
{
    public static class DiagnosticsLoggerExceptionExtensions
    {
        public static void LogException(this IDiagnosticsLogger self, Exception exception, object entity = null)
        {
            self.LogException(Severity.Error, exception);
        }

        public static void LogException(this IDiagnosticsLogger self,Severity severity, Exception exception, object entity = null)
        {
            JObject json;
            if (entity != null)
            {
                json = entity as JObject ?? self.Converter.ToJObject(entity);
                json.Merge(self.Converter.ToJObject(exception));
            }
            else
            {
                json = self.Converter.ToJObject(exception);
            }
            self.LogFailure(severity, json);
        }
    }
}