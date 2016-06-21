using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        IDiagnosticsLogger Diag { get; }

        IPerformanceTracker Track(string type, params object[] args);
        IPerformanceTracker TrackRequest(HttpRequestMessage request);
        IPerformanceTracker TrackTask(string name);

        void TrackAction(Action action, params object[] args);
        void TrackAction(string name, Action action, params object[] args);

        T TrackFunction<T>(Func<T> func, params object[] args);
        T TrackFunction<T>(string name, Func<T> func, params object[] args);

        void LogSingleEvent(string type, long elapsed, params object[] args);
    }

    //TODO: Injectable service.
    public static class Correlator
    {
        private const string CORRELATION_KEY = "CORRELATION_KEY";
        private const string EMPTY = "00000000";

        public static void Set(Guid id)
        {
            CallContext.LogicalSetData(CORRELATION_KEY, Hash(id.ToByteArray(), 5));
        }

        public static string Get()
        {
            string ctx = (string)CallContext.LogicalGetData(CORRELATION_KEY);
            return ctx ?? EMPTY;
        }

        private static string Hash(byte[] bytes, int size)
        {
            using (SHA1 hasher = SHA1.Create())
            {
                byte[] hash = hasher.ComputeHash(bytes);
                return string.Join(string.Empty, hash.Take(size).Select(b => b.ToString("x2")));
            }
        }
    }
    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly ILogWriter writer;

        public bool Enabled { get; }
        public IDiagnosticsLogger Diag { get; }

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration, IDiagnosticsLogger diagnostics)
        {
            Diag = diagnostics;
            if (configuration.Diagnostics?.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;

            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker TrackRequest(HttpRequestMessage request)
        {
            Correlator.Set(request.GetCorrelationId());
            return Track("request", request.Method.Method, request.RequestUri.ToString());
        }

        public IPerformanceTracker TrackTask(string name) => Track("task", name);

        public IPerformanceTracker Track(string type, params object[] args)
        {
            return !Enabled 
                ? NullPerformanceTracker.SharedInstance
                : PerformanceTracker.Create(LogPerformanceEvent, Correlator.Get(), type, args);
        }

        public void TrackAction(Action action, params object[] args) => TrackAction(action.Method.Name, action);

        public void TrackAction(string name, Action action, params object[] args)
        {
            using (Track(name, args))
                action();
        }

        public T TrackFunction<T>(Func<T> func, params object[] args) => TrackFunction(func.Method.Name, func, args);

        public T TrackFunction<T>(string name, Func<T> func, params object[] args)
        {
            T output = default (T);
            TrackAction(name, () => output = func(), args);
            return output;
        }

        public void LogSingleEvent(string type, long elapsed, params object[] args)
        {
            PerformanceEvent.Execute(LogPerformanceEvent, type, Correlator.Get(), elapsed, args);
        }

        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }
    }
}