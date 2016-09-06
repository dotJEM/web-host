using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics
{
    public interface IDiagnosticsLogger
    {
        IJsonConverter Converter { get; }

        void Log(string contentType, Severity severity, object entity);
        void Log(string contentType, Severity severity, string message, object entity = null);
    }
}