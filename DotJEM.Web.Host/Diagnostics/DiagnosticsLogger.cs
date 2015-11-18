using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Providers.Concurrency;
using DotJEM.Web.Host.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics
{
    public interface IDiagnosticsLogger
    {
        IJsonConverter Converter { get; }

        void Log(string contentType, Severity severity, object entity);
        void Log(string contentType, Severity severity, string message, object entity = null);
    }

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

    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public const string ContentTypeIncident = "incident";
        public const string ContentTypeWarning = "warning";
        public const string ContentTypeFailure = "failure";

        private readonly Lazy<IStorageArea> area;
        private readonly Lazy<IStorageIndexManager> manager;

        public IStorageArea Area { get { return area.Value; } }
        public IStorageIndexManager Manager { get { return manager.Value; } }
        public IJsonConverter Converter { get; private set; }

        public DiagnosticsLogger(Lazy<IStorageContext> context, Lazy<IStorageIndexManager> manager, IJsonConverter converter)
        {
            this.area = new Lazy<IStorageArea>(() => context.Value.Area("diagnostic"));
            this.manager = manager;
            this.Converter = converter;
        }

        public void Log(string contentType, Severity severity, object entity = null)
        {
            JObject json = EnsureJson(entity);
            json["host"] = Environment.MachineName;
            json["severity"] = Converter.FromObject(severity);
            json["stackTrace"] = JArray.FromObject(BuildStackTrace().ToArray());
            json = Area.Insert(contentType, json);
            Manager.QueueUpdate(json);
        }

        public void Log(string contentType, Severity severity, string message, object entity = null)
        {
            JObject json = EnsureJson(entity);
            json["message"] = message;
            Log(contentType, severity, json);
        }
        
        private JObject EnsureJson(object entity)
        {
            return entity == null ? new JObject() : (entity as JObject ?? Converter.ToJObject(entity));
        }

        internal static IEnumerable<string> BuildStackTrace()
        {
            IEnumerable<StackFrame> frames = new StackTrace().GetFrames();
            if (frames == null)
                yield break;

            foreach (StackFrame frame in frames)
            {
                StringBuilder builder = new StringBuilder(1024);
                MethodBase method = frame.GetMethod();
                if (method != null)
                {
                    builder.Append("   at ");
                    Type declaringType = method.DeclaringType;
                    if (declaringType != (Type)null)
                    {
                        builder.Append(declaringType.FullName.Replace('+', '.'));
                        builder.Append(".");
                    }

                    builder.Append(method.Name);
                    if (method is MethodInfo && method.IsGenericMethod)
                    {
                        builder.Append("[");
                        builder.Append(string.Join(", ", method.GetGenericArguments().Select(g => g.Name)));
                        builder.Append("]");
                    }

                    builder.Append("(");
                    builder.Append(string.Join(", ", method.GetParameters().Select(p => (p.ParameterType != null ? p.ParameterType.Name : "<UnknownType>") + " " + p.Name)));
                    builder.Append(")");

                    if (frame.GetILOffset() != -1)
                    {
                        try
                        {
                            string fileName = frame.GetFileName();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                builder.AppendFormat(" in {0}:line {1}", fileName, frame.GetFileLineNumber());
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
                yield return builder.ToString();
            }
        }
    }
}