namespace DotJEM.Web.Host.Diagnostics.Ext
{
    public static class DiagnosticsLoggerWarningExtensions
    {
        private const string ContentType = "warning";

        public static void LogWarning(this IDiagnosticsLogger self, Severity severity, object entity)
        {
            self.Log(ContentType, severity, entity);
        }

        public static void LogWarning(this IDiagnosticsLogger self, Severity severity, string message, object entity = null)
        {
            self.Log(ContentType, severity, message, entity);
        }
    }
}