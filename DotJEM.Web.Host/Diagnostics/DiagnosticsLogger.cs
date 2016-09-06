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
    public class DiagnosticsLogger : IDiagnosticsLogger
    {
        public const string ContentTypeIncident = "incident";
        public const string ContentTypeWarning = "warning";
        public const string ContentTypeFailure = "failure";

        private readonly Lazy<IStorageArea> area;
        private readonly Lazy<IStorageIndexManager> manager;

        public IStorageArea Area => area.Value;
        public IStorageIndexManager Manager => manager.Value;
        public IJsonConverter Converter { get; }

        public DiagnosticsLogger(Lazy<IStorageContext> context, Lazy<IStorageIndexManager> manager, IJsonConverter converter)
        {
            area = new Lazy<IStorageArea>(() => context.Value.Area("diagnostic"));
            this.manager = manager;
            Converter = converter;
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
                    if (declaringType != null)
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