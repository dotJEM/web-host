using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Correlations;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly ILogWriter writer;

        public bool Enabled { get; }

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration)
        {
            if (configuration.Diagnostics?.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;
            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public IPerformanceTracker Track(string type, params object[] args)
        {
            if (!Enabled)
                return NullPerformanceTracker.SharedInstance;

            return PerformanceTracker.Create(LogPerformanceEvent, new CorrelationFlow(), type, args);
        }

        public void LogSingleEvent(string type, long elapsed, params object[] args)
        {
            PerformanceEvent.Execute(LogPerformanceEvent, type, new CorrelationFlow(), elapsed, args);
        }

        private void LogPerformanceEvent(string message)
        {
            if (!Enabled)
                return;

            writer.Write(message);
        }
        private void ScopeCompleted(CapturedScope obj)
        {

        }

        public ICorrelationScope StartCorrelationScope() => StartCorrelationScope(Guid.NewGuid());

        public ICorrelationScope StartCorrelationScope(Guid scopeid) => new CorrelationScope(scopeid, ScopeCompleted);

    }

    public interface ICorrelationFlow : IDisposable
    {
        ICorrelationFlow Parent { get; }
        string Hash { get; }
        Guid Uid { get; }
        ICorrelationFlow Capture(DateTime time, long elapsed, string type, string identity, string[] arguments);
    }

    public class CorrelationFlow : ICorrelationFlow
    {
        private const string KEY = "FLOW_CONTEXT_KEY_D5A5BDE3"; 

        public static ICorrelationFlow Current => (ICorrelationFlow)CallContext.LogicalGetData(KEY);

        private readonly ICorrelation scope;
        private bool disposed;

        public ICorrelationFlow Parent { get; }

        public string Hash => scope.Hash;
        public Guid Uid { get; } = Guid.NewGuid();

        public CorrelationFlow()
        {
            Parent = Current;
            CallContext.LogicalSetData(KEY, this);
            scope = CorrelationScope.Current.Flow(this);
        }

        public ICorrelationFlow Capture(DateTime time, long elapsed, string type, string identity, string[] args)
        {
            new CapturedFlow(Uid, time, elapsed, type, identity, args);
            throw new NotImplementedException();
            //return this;
        }

        public void Dispose()
        {
            if(disposed)
                return;
            
            disposed = true;
            CallContext.LogicalSetData(KEY, Parent);
        }
    }
}