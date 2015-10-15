using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
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

        void TrackAction(Action action);
        void TrackAction(string name, Action action);

        T TrackFunction<T>(Func<T> func);
        T TrackFunction<T>(string name, Func<T> func);
    }

    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly ILogWriter writer;

        public bool Enabled { get; }
        public IDiagnosticsLogger Diag { get; }

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration, IDiagnosticsLogger diagnostics)
        {
            Diag = diagnostics;
            //TODO: Null logger pattern
            if (configuration.Diagnostics == null || configuration.Diagnostics.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;

            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker TrackRequest(HttpRequestMessage request)
        {
            return Track("request", request.Method.Method, request.RequestUri.ToString());
        }

        public IPerformanceTracker TrackTask(string name)
        {
            return Track("task", name);
        }

        public IPerformanceTracker Track(string type, params object[] args)
        {
            return new PerformanceTracker(LogPerformanceEvent, type, args);
        }

        public void TrackAction(Action action)
        {
            TrackAction(action.Method.Name, action);
        }

        public void TrackAction(string name, Action action)
        {
            var methodName = action.Method.Name;
            var tracker = TrackTask(name);
            action();
            tracker.Commit(methodName);
        }

        public T TrackFunction<T>(Func<T> func)
        {
            return TrackFunction(func.Method.Name, func);
        }

        public T TrackFunction<T>(string name, Func<T> func)
        {
            T output = default (T);
            TrackAction(name, () => output = func());
            return output;
        }


        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }
    }
}