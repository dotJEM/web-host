namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public interface IInfoStreamEvent
{
    string Level { get; }
    string Message { get; }
}