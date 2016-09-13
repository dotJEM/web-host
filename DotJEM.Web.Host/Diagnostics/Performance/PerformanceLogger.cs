using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using Castle.Components.DictionaryAdapter;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Diagnostics.Performance.Correlations;
using DotJEM.Web.Host.Diagnostics.Performance.Trackers;
using DotJEM.Web.Host.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            //obj.Await
            //TODO await completion

            JObject json = JObject.FromObject(obj);

            string str = json.ToString(Formatting.Indented);

            Debug.WriteLine(str);
        }

        public ICorrelationScope StartCorrelationScope() => StartCorrelationScope(Guid.NewGuid());

        public ICorrelationScope StartCorrelationScope(Guid scopeid) => new CorrelationScope(scopeid, ScopeCompleted);

    }

    public interface ICorrelationFlow : IDisposable
    {
        ICorrelationFlow Parent { get; }
        string Hash { get; }
        Guid Uid { get; }
        IEnumerable<CapturedFlow> Flows { get; }
        ICorrelationFlow Capture(DateTime time, long elapsed, string type, string identity, string[] arguments);

        void Collect();
    }

    public class CorrelationFlow : ICorrelationFlow
    {
        private const string KEY = "FLOW_CONTEXT_KEY_D5A5BDE3"; 

        public static ICorrelationFlow Current => (ICorrelationFlow)CallContext.LogicalGetData(KEY);

        private readonly ICorrelation scope;
        private bool collected;
        private bool completed;

        public ICorrelationFlow Parent { get; }

        public string Hash => scope.Hash;
        public Guid Uid { get; } = Guid.NewGuid();

        private readonly List<CapturedFlow> flows = new List<CapturedFlow>();

        public IEnumerable<CapturedFlow> Flows => flows;

        public CorrelationFlow()
        {
            Parent = Current;
            CallContext.LogicalSetData(KEY, this);
            scope = CorrelationScope.Current.Flow(this);
        }

        public ICorrelationFlow Capture(DateTime time, long elapsed, string type, string identity, string[] args)
        {
            flows.Add(new CapturedFlow(Parent?.Uid ?? Guid.Empty, Uid, Hash, time, elapsed, type, identity, args));
            return this;
        }

        public void Collect()
        {
            if (collected)
                return;

            collected = true;
            CallContext.LogicalSetData(KEY, Parent);
        }

        public void Complete() => completed = true;
        public void Dispose() => Complete();
    }
}