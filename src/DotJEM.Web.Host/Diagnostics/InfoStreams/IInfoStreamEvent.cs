namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public interface IInfoStreamEvent
{
    string Level { get; }
    string Message { get; }
    string CallerMemberName { get; }
    string CallerFilePath { get; }
    int CallerLineNumber { get; }
}