namespace DotJEM.Web.Host.Diagnostics.Ext
{
    public static class DiagnosticsLoggerFailureExtensions
    {
        private const string ContentType = "failure";

        public static void LogFailure(this IDiagnosticsLogger self, Severity severity, object entity)
        {
            self.Log(ContentType, severity, entity);
        }

        public static void LogFailure(this IDiagnosticsLogger self, Severity severity, string message, object entity = null)
        {
            self.Log(ContentType, severity, message, entity);
        }
    }
}