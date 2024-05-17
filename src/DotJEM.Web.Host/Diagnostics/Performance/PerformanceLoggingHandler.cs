using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance;

public interface ITelemetryX
{
    Activity CreateActivity(string name, ActivityKind kind);
    Activity CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown);
    Activity CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown);
    Activity CreateRootActivity(string name, ActivityKind kind);

    Activity StartActivity([CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal);
    Activity StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset());
    Activity StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset());
    Activity StartActivity(ActivityKind kind, ActivityContext parentContext = new ActivityContext(), IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset(), string name = "");
    Activity StartRootActivity([CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal);
}

public class TelemetryX : ITelemetryX
{
    private readonly ActivitySource source;

    public TelemetryX(string serviceName = null, string version="")
    {
        serviceName ??= Process.GetCurrentProcess().ProcessName;
        source = new ActivitySource(serviceName, version);
    }

    public Activity CreateActivity(string name, ActivityKind kind) => source.CreateActivity(name, kind);
    public Activity CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown) => source.CreateActivity(name, kind, parentContext, tags, links, idFormat);
    public Activity CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown) => source.CreateActivity(name, kind, parentId, tags, links, idFormat);
    public Activity CreateRootActivity(string name, ActivityKind kind)
    {
        using (new CaptureResetActivity())
            return source.CreateActivity(name, kind);
    }

    public Activity StartActivity([CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal) => source.StartActivity(name, kind);
    public Activity StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset()) => source.StartActivity(name, kind, parentContext, tags, links, startTime);
    public Activity StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset()) => source.StartActivity(name, kind, parentId, tags, links, startTime);
    public Activity StartActivity(ActivityKind kind, ActivityContext parentContext = new ActivityContext(), IEnumerable<KeyValuePair<string, object>> tags = null, IEnumerable<ActivityLink> links = null, DateTimeOffset startTime = new DateTimeOffset(), string name = "") => source.StartActivity(kind, parentContext, tags, links, startTime, name);
    public Activity StartRootActivity(string name = "", ActivityKind kind = ActivityKind.Internal)
    {
        using (new CaptureResetActivity())
            return source.StartActivity(name, kind);
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