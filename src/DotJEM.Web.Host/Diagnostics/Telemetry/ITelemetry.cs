using DotJEM.Web.Host.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotJEM.Web.Host.Diagnostics.Telemetry;

//TODO: this is meant to make 
public interface ITelemetry
{
    IActivitySource ActivitySource { get; }
    IEventSource EventSource { get; }

}


public class NullTelemetry : ITelemetry
{
    public IActivitySource ActivitySource { get; } = new NullActivitySource();
    public IEventSource EventSource { get; } = new NullEventSource();
}

public static class TelemetryExtensions
{
    public static void TrackAction(this ITelemetry self, Action action)
        => self.ActivitySource.TrackAction(action, action.Method.Name);
    public static void TrackAction(this ITelemetry self, Action action, string name)
        => self.ActivitySource.TrackAction(action, name);
    public static T TrackFunction<T>(this ITelemetry self, Func<T> func, string name)
        => self.ActivitySource.TrackFunction(func, name);

    public static IActivity CreateRootActivity(this ITelemetry self, string name, ActivityKind kind)
        => self.ActivitySource.CreateRootActivity(name, kind);
    public static IActivity StartRootActivity(this ITelemetry self, string name = "", ActivityKind kind = ActivityKind.Internal)
        => self.ActivitySource.StartRootActivity(name, kind);



    public static void TrackTask(this ITelemetry self, Task task, string name)
    {
        using IActivity activity = self.ActivitySource.StartActivity($"task:{name}");
        Sync.Await(task);
    }
}

public static class ActivitySourceExtensions
{
    public static void TrackAction(this IActivitySource self, Action action, string name)
    {
        if (self is NullActivitySource)
        {
            action();
            return;
        }

        using IActivity activity = self.StartActivity(name);
        action();
    }
    public static T TrackFunction<T>(this IActivitySource self, Func<T> func, string name)
    {
        if (self is NullActivitySource)
            return func();

        using IActivity activity = self.StartActivity(name);
            return func();
    }



    public static IActivity CreateRootActivity(this IActivitySource self, string name, ActivityKind kind)
    {
        using (new CaptureResetActivity())
            return self.StartActivity(name, kind);

    }

    public static IActivity StartRootActivity(this IActivitySource self, string name = "", ActivityKind kind = ActivityKind.Internal)
    {
        using (new CaptureResetActivity())
            return self.StartActivity(name, kind);
    }

    private class CaptureResetActivity : IDisposable
    {
        private readonly Activity current;

        public CaptureResetActivity()
        {
            current = Activity.Current;
            Activity.Current = null;
        }

        public void Dispose() => Activity.Current = current;
    }
}

public interface IActivitySource
{
    string Name { get; }
    string Version { get; }

    bool HasListeners();
    IActivity CreateActivity(string name, ActivityKind kind);
    IActivity CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown);
    IActivity CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown);
    IActivity StartActivity(string name = "", ActivityKind kind = ActivityKind.Internal);
    IActivity StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new ());
    IActivity StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new ());
    IActivity StartActivity(ActivityKind kind, ActivityContext parentContext = new (), IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new (), string name = "");

    void Dispose();
}

public class ForwardingActivitySource : IActivitySource
{
    private readonly ActivitySource wrapped;
    public string Name => wrapped.Name;
    public string Version => wrapped.Version;

    public ForwardingActivitySource(ActivitySource wrapped)
    {
        this.wrapped = wrapped;
    }

    public bool HasListeners() => wrapped.HasListeners();
    public IActivity CreateActivity(string name, ActivityKind kind)
        => new ProxyActivity(wrapped.CreateActivity(name, kind));
    public IActivity CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown) 
        => new ProxyActivity(wrapped.CreateActivity(name, kind, parentContext, tags, links, idFormat));
    public IActivity CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown) 
        => new ProxyActivity(wrapped.CreateActivity(name, kind, parentId, tags, links, idFormat));
    public IActivity StartActivity(string name = "", ActivityKind kind = ActivityKind.Internal)
        => new ProxyActivity(wrapped.StartActivity(name, kind));
    public IActivity StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset()) 
        => new ProxyActivity(wrapped.StartActivity(name, kind, parentContext, tags, links, startTime));
    public IActivity StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset()) 
        => new ProxyActivity(wrapped.StartActivity(name, kind, parentId, tags, links, startTime));
    public IActivity StartActivity(ActivityKind kind, ActivityContext parentContext = new ActivityContext(), IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset(), string name = "")
        => new ProxyActivity(wrapped.StartActivity(kind, parentContext, tags, links, startTime, name));

    public void Dispose() => wrapped.Dispose();

}

public class NullActivitySource : IActivitySource
{
    public string Name => string.Empty;
    public string Version => string.Empty;
    public bool HasListeners() => false;
    public IActivity CreateActivity(string name, ActivityKind kind) => NullActivity.Default;

    public IActivity CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown)
    {
        return NullActivity.Default;
    }

    public IActivity CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown)
    {
        return NullActivity.Default;
    }

    public IActivity StartActivity(string name = "", ActivityKind kind = ActivityKind.Internal)
    {
        return NullActivity.Default;
    }

    public IActivity StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset())
    {
        return NullActivity.Default;
    }

    public IActivity StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset())
    {
        return NullActivity.Default;
    }

    public IActivity StartActivity(ActivityKind kind, ActivityContext parentContext = new ActivityContext(), IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset(), string name = "")
    {
        return NullActivity.Default;
    }

    public void Dispose() { }
}

public interface IEventSource
{
    string Name { get; }
    Guid Guid { get; }
    EventSourceSettings Settings { get; }
    Exception ConstructionException { get; }
    bool IsEnabled();
    bool IsEnabled(EventLevel level, EventKeywords keywords);
    bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel);
    string GetTrait(string key);
    void Dispose();
    void Write(string eventName);
    void Write(string eventName, EventSourceOptions options);
    void Write<T>(string eventName, T data);
    void Write<T>(string eventName, EventSourceOptions options, T data);
    void Write<T>(string eventName, ref EventSourceOptions options, ref T data);
    void Write<T>(string eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref T data);
}

public class ForwardingEventSource : IEventSource
{
    private readonly EventSource wrapped;

    public string Name => wrapped.Name;

    public Guid Guid => wrapped.Guid;

    public EventSourceSettings Settings => wrapped.Settings;

    public Exception ConstructionException => wrapped.ConstructionException;
    public ForwardingEventSource(EventSource wrapped)
    {
        this.wrapped = wrapped;
    }

    public bool IsEnabled() => wrapped.IsEnabled();
    public bool IsEnabled(EventLevel level, EventKeywords keywords) => wrapped.IsEnabled(level, keywords);
    public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel) => wrapped.IsEnabled(level, keywords, channel);
    public string GetTrait(string key) => wrapped.GetTrait(key);
    public void Dispose() => wrapped.Dispose();

    public void Write(string eventName) => wrapped.Write(eventName);
    public void Write(string eventName, EventSourceOptions options) => wrapped.Write(eventName, options);
    public void Write<T>(string eventName, T data) => wrapped.Write(eventName, data);
    public void Write<T>(string eventName, EventSourceOptions options, T data) => wrapped.Write(eventName, options, data);
    public void Write<T>(string eventName, ref EventSourceOptions options, ref T data) => wrapped.Write(eventName, ref options, ref data);
    public void Write<T>(string eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref T data) => wrapped.Write(eventName, ref options, ref activityId, ref relatedActivityId, ref data);

}

public class NullEventSource : IEventSource
{
    public string Name => string.Empty;
    public Guid Guid => Guid.Empty;
    public EventSourceSettings Settings => EventSourceSettings.Default;
    public Exception ConstructionException { get; }
    public bool IsEnabled() => false;
    public bool IsEnabled(EventLevel level, EventKeywords keywords) => false;

    public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel) => false;

    public string GetTrait(string key) => string.Empty;

    public void Dispose() {}

    public void Write(string eventName)
    {
    }

    public void Write(string eventName, EventSourceOptions options)
    {
    }

    public void Write<T>(string eventName, T data)
    {
    }

    public void Write<T>(string eventName, EventSourceOptions options, T data)
    {
    }

    public void Write<T>(string eventName, ref EventSourceOptions options, ref T data)
    {
    }

    public void Write<T>(string eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref T data)
    {
    }
}

public interface IActivity : IDisposable
{

}

public class ProxyActivity(Activity activity) : IActivity
{
    public void Dispose()
    {
        activity?.Dispose();
    }
}

public class NullActivity : IActivity
{
    public static NullActivity Default { get; } = new NullActivity();

    public void Dispose() {}
}