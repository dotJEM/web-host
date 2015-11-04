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

        void TrackAction(Action action, params object[] args);
        void TrackAction(string name, Action action, params object[] args);

        T TrackFunction<T>(Func<T> func, params object[] args);
        T TrackFunction<T>(string name, Func<T> func, params object[] args);
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

        public void TrackAction(Action action, params object[] args)
        {
            TrackAction(action.Method.Name, action);
        }

        public void TrackAction(string name, Action action, params object[] args)
        {
            using (Track(name, args))
            {
                action();
            }
        }

        public T TrackFunction<T>(Func<T> func, params object[] args)
        {
            return TrackFunction(func.Method.Name, func, args);
        }

        public T TrackFunction<T>(string name, Func<T> func, params object[] args)
        {
            T output = default (T);
            TrackAction(name, () => output = func(), args);
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