using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.Correlation;
using DotJEM.Diagnostic.DataProviders;
using DotJEM.Diagnostic.Writers;
using DotJEM.Json.Index.Util;
using DotJEM.Web.Host.Configuration.Elements;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface ILoggerFactory
    {
        ILogger Create();
    }

    public class LoggerFactory : ILoggerFactory
    {
        private readonly IPathResolver resolver;
        private readonly IWebHostConfiguration configuration;
        private readonly IPerformanceLoggingCustomDataProviderManager customDataProviderManager;

        public LoggerFactory(IWebHostConfiguration configuration, IPathResolver resolver, IPerformanceLoggingCustomDataProviderManager customDataProviderManager)
        {
            this.configuration = configuration;
            this.resolver = resolver;
            this.customDataProviderManager = customDataProviderManager;
        }

        public ILogger Create()
        {
            if (configuration.Diagnostics?.Performance == null)
                return new NullLogger();

            PerformanceConfiguration config = configuration.Diagnostics.Performance;
            var writer = new QueuingTraceWriter(resolver.MapPath(config.Path), AdvConvert.ConvertToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
            return new HighPrecisionLogger(new TraceEventCollector(writer), customDataProviderManager.Providers);
        }
    }

    public interface IPerformanceLoggingCustomDataProviderManager
    {
        (string name, ICustomDataProvider provider)[] Providers { get; }
    }


    public interface IInsertionControl
    {
        IPerformanceLoggingCustomDataProviderManager After(string name);
        IPerformanceLoggingCustomDataProviderManager Before(string name);
        IPerformanceLoggingCustomDataProviderManager BeforeAll();
        IPerformanceLoggingCustomDataProviderManager AfterAll();
    }

    public interface IRemovalControl
    {
        IPerformanceLoggingCustomDataProviderManager First();
        IPerformanceLoggingCustomDataProviderManager Last();
        IPerformanceLoggingCustomDataProviderManager Item(string name);
        IPerformanceLoggingCustomDataProviderManager All();
    }

    public class PerformanceLoggingCustomDataProviderManager : IPerformanceLoggingCustomDataProviderManager
    {
        private class InsertionControl : IInsertionControl
        {
            private readonly PerformanceLoggingCustomDataProviderManager manager;
            private readonly string name;
            private readonly ICustomDataProvider provider;

            public InsertionControl(PerformanceLoggingCustomDataProviderManager manager, string name, ICustomDataProvider provider)
            {
                this.manager = manager;
                this.name = name;
                this.provider = provider;
            }

            public IPerformanceLoggingCustomDataProviderManager After(string name)
            {
                manager.providers.AddAfter(manager.Find(name), new Entry(this.name, provider));
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager Before(string name)
            {
                manager.providers.AddBefore(manager.Find(name), new Entry(this.name, provider));
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager BeforeAll()
            {
                manager.providers.AddFirst(new Entry(name, provider));
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager AfterAll()
            {
                manager.providers.AddLast(new Entry(name, provider));
                return manager;
            }
        }

        private class RemovalControl : IRemovalControl {
            private readonly PerformanceLoggingCustomDataProviderManager manager;

            public RemovalControl(PerformanceLoggingCustomDataProviderManager manager)
            {
                this.manager = manager;
            }

            public IPerformanceLoggingCustomDataProviderManager First()
            {
                manager.providers.RemoveFirst();
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager Last()
            {
                manager.providers.RemoveLast();
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager Item(string name)
            {
                manager.providers.Remove(manager.Find(name));
                return manager;
            }

            public IPerformanceLoggingCustomDataProviderManager All()
            {
                manager.providers.Clear();
                return manager;
            }
        }

        private class Entry
        {
            public string Name { get; }
            public ICustomDataProvider Value { get; set; }

            public Entry(string name, ICustomDataProvider value)
            {
                Name = name;
                Value = value;
            }
        }

        private readonly LinkedList<Entry> providers = new LinkedList<Entry>();

        public (string name, ICustomDataProvider provider)[] Providers => providers.Select(entry => (entry.Name, entry.Value)).ToArray();

        public PerformanceLoggingCustomDataProviderManager()
        {
            Insert("Identity", new IdentityProvider()).AfterAll();
            Insert("Thread", new ThreadIdProvider()).AfterAll();
            Insert("Process", new ProcessIdProvider()).AfterAll();
        }

        public IInsertionControl Insert(string name, ICustomDataProvider provider) => new InsertionControl(this, name, provider);

        public IRemovalControl Remove() => new RemovalControl(this);

        public IPerformanceLoggingCustomDataProviderManager Replace(string name, ICustomDataProvider provider)
        {
            Find(name).Value.Value = provider;
            return this;
        }

        private LinkedListNode<Entry> Find(string name)
        {
            LinkedListNode<Entry> node = providers.First;
            while (node != null && node.Value.Name != name)
                node = node.Next;
            if (node == null)
                throw new KeyNotFoundException($"Could not find any providers named {name}...");
            return node;
        }
    }

    public class NullLogger : ILogger
    {
        public Task LogAsync(string type, object customData) => Task.CompletedTask;
    }

    public class PerformanceLoggingHandler : DelegatingHandler
    {
        private readonly ILogger logger;
        public PerformanceLoggingHandler(ILogger logger)
        {
            this.logger = logger;
        }

        public PerformanceLoggingHandler(ILogger logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!logger.IsEnabled())
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            using (new CorrelationScope(request.GetCorrelationId()))
            {
                using (IPerformanceTracker tracker = logger.TrackRequest(request))
                {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(true);
                    tracker.Commit(new { statusCode = response.StatusCode });
                    return response;
                }
            }
        }
    }

    public static class LoggerExtensions
    {
        public static bool IsEnabled(this ILogger self) => !(self is NullLogger);

        public static IPerformanceTracker TrackRequest(this ILogger self, HttpRequestMessage request)
        {
            return self.Track("request", new
            {
                method = request.Method.Method,
                uri = request.RequestUri.ToString(),
                contentType = request.Content.Headers.ContentType,
                contentLength = request.Content.Headers.ContentLength
            });
        }
        public static IPerformanceTracker TrackTask(this ILogger self, string name)
        {
            return self.Track("task", new { name });
        }
    }
}
