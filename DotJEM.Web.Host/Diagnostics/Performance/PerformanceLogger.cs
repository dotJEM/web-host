using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Correlations;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        IDiagnosticsLogger Diagnostic { get; }
        IPerformanceTracker Track(string type, params object[] args);

        void LogSingleEvent(string type, long elapsed, params object[] args);

        ICorrelationScope StartCorrelationScope();
        ICorrelationScope StartCorrelationScope(Guid scopeid);
    }

    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly ILogWriter writer;

        public bool Enabled { get; }
        public IDiagnosticsLogger Diagnostic { get; }

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration, IDiagnosticsLogger diagnostics)
        {
            Diagnostic = diagnostics;
            if (configuration.Diagnostics?.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;
            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker Track(string type, params object[] args)
        {
            return !Enabled 
                ? NullPerformanceTracker.SharedInstance
                : PerformanceTracker.Create(LogPerformanceEvent, CorrelationScope.Current.Hash, type, args);
        }
        
        public void LogSingleEvent(string type, long elapsed, params object[] args)
        {
            PerformanceEvent.Execute(LogPerformanceEvent, type, CorrelationScope.Current.Hash, elapsed, args);
        }

        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }

        public ICorrelationScope StartCorrelationScope() => StartCorrelationScope(Guid.NewGuid());

        public ICorrelationScope StartCorrelationScope(Guid scopeid) => new CorrelationScope(scopeid);
    }
    
    public static class PerformanceLoggerAsyncExtensions
    {
        public static async Task<T> TrackTask<T>(this IPerformanceLogger self, string name, Task<T> task, params object[] args)
        {
            using (self.Track(name, args))
                return await task;
        }
    }

    public static class PerformanceLoggerHttpExtensions
    {
        public static IPerformanceTracker TrackRequest(this IPerformanceLogger self, HttpRequestMessage request)
        {
            return self.Track("request", request.Method.Method, request.RequestUri.ToString(), request.Content.Headers.ContentLength);
        }
    }

    public static class PerformanceLoggerBasicExtensions
    {
        public static IPerformanceTracker TrackTask(this IPerformanceLogger self, string name) => self.Track("task", name);
    }

    public static class PerformanceLoggerFunctionalExtensions
    {
        public static T TrackFunction<T>(this IPerformanceLogger self, string name, Func<T> func, params object[] args)
        {
            T output = default(T);
            self.TrackAction(name, () => output = func(), args);
            return output;
        }
        public static T TrackFunction<T>(this IPerformanceLogger self, Func<T> func, params object[] args) => self.TrackFunction(func.Method.Name, func, args);

        public static void TrackAction(this IPerformanceLogger self, Action action, params object[] args) => self.TrackAction(action.Method.Name, action);

        public static void TrackAction(this IPerformanceLogger self, string name, Action action, params object[] args)
        {
            using (self.Track(name, args))
                action();
        }
    }
}