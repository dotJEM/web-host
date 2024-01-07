using System;
using System.Linq;
using DotJEM.Json.Index2.Management;
using DotJEM.Json.Index2.Management.Snapshots.Zip.Meta;
using DotJEM.Json.Index2.Management.Tracking;
using DotJEM.Json.Index2.Results;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Scheduler;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Initialization;

public interface IInitializationTracker : IObserver<IInfoStreamEvent>
{
    event EventHandler<EventArgs> Progress;

    JObject Json { get; }
    string Message { get; }
    ITrackerState State { get; }
    double Percent { get; }
    bool Completed { get; }
    DateTime StarTime { get; }
    TimeSpan Duration { get; }

    void SetProgress(double percent);
    void SetProgress(string message, params object[] args);
    void SetProgress(double percent, string message, params object[] args);

    void SetProgress(JObject json, double percent);
    void SetProgress(JObject json, string message, params object[] args);
    void SetProgress(JObject json, double percent, string message, params object[] args);
    void Complete();
}

public class InitializationTracker : IInitializationTracker
{
    public event EventHandler<EventArgs> Progress;

    private JObject jsonData = new JObject();

    public JObject Json => CreateJObject();
    public string Message { get; private set; } = "";
    public ITrackerState State { get; private set; } = null;
    public double Percent { get; private set; } = 0;
    public bool Completed { get; private set; } = false;
    public DateTime StarTime { get; } = DateTime.Now;
    public TimeSpan Duration => DateTime.Now - StarTime;

    private JObject CreateJObject()
    {
        JObject json = JObject.FromObject(new
        {
            completed = Completed,
            percent = Percent,
            starTime = StarTime,
            duration = Duration,
            message = Message,
            metaData = jsonData
        });
        return json;
    }

    public void SetProgress(double percent)
        => SetProgress(percent, Message);

    public void SetProgress(string message, params object[] args)
        => SetProgress(Percent, message, args);

    public void SetProgress(double percent, string message, params object[] args)
        => SetProgress(jsonData, Percent, message, args);

    public void SetProgress(JObject json, double percent)
        => SetProgress(json, percent, Message);

    public void SetProgress(JObject json, string message, params object[] args)
        => SetProgress(json, Percent, message, args);

    public void SetProgress(JObject json, double percent, string message, params object[] args)
    {
        Percent = percent;
        Message = args.Any() ? string.Format(message, args) : message;
        jsonData = json;
        OnProgress();
    }

    public void Complete()
    {
        Percent = 100;
        Completed = true;
        OnProgress();
    }

    protected virtual void OnProgress()
    {
        Progress?.Invoke(this, EventArgs.Empty);
    }

    public void OnNext(IInfoStreamEvent value)
    {
        switch (value)
        {
            case IInfoStreamExceptionEvent infoStreamExceptionEvent:
                break;
            case ZipSnapshotEvent zipSnapshotEvent:
                break;
            case StorageIngestStateInfoStreamEvent storageIngestStateInfoStreamEvent:
                break;
            case StorageObserverInfoStreamEvent storageObserverInfoStreamEvent:
                break;
            case TrackerStateInfoStreamEvent state:
                SetProgress(state.Message);
                State = state.State;
                switch (state.State)
                {
                    case SnapshotRestoreState snapshotState:
                        break;
                    case StorageIngestState storageIngestState:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case SearchInfoStreamEvent searchInfoStreamEvent:
                break;
            case ZipFileEvent zipFileEvent:
                break;
            case InfoStreamExceptionEvent infoStreamExceptionEvent1:
                break;
            case TaskCompletedInfoStreamEvent taskCompletedInfoStreamEvent:
                break;
            case InfoStreamEvent infoStreamEvent:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }
}