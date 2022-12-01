using System;

namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public class InfoStreamExceptionEvent : InfoStreamEvent
{
    public Exception Exception { get; }

    public InfoStreamExceptionEvent(string level, string message, string callerMemberName, string callerFilePath, int callerLineNumber, Exception exception)
        : base(level, message, callerMemberName, callerFilePath, callerLineNumber)
    {
        Exception = exception;
    }
}