namespace DotJEM.Web.Host.Diagnostics.Ext
{
    public static class DiagnosticsLoggerTraceExtensions
    {
        private const string ContentType = "trace";

        public static void LogTrace(this IDiagnosticsLogger self, Severity severity, object entity)
        {
            self.Log(ContentType, severity, entity);
        }

        public static void LogTrace(this IDiagnosticsLogger self, Severity severity, string message, object entity = null)
        {
            self.Log(ContentType, severity, message, entity);
        }
    }
}