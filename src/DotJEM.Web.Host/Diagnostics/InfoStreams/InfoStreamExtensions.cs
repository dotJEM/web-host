using System;
using System.Runtime.CompilerServices;

namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public static class InfoStreamExtensions
{
    public static void WriteError(this IInfoStream self, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
    {
        self.WriteEvent(new InfoStreamExceptionEvent("ERROR", exception.Message, callerMemberName, callerFilePath, callerLineNumber, exception));
    }

    public static void WriteError(this IInfoStream self, string message, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
    {
        self.WriteEvent(new InfoStreamExceptionEvent("ERROR", message, callerMemberName, callerFilePath, callerLineNumber, exception));
    }

    public static void WriteInfo(this IInfoStream self, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
    {
        self.WriteEvent(new InfoStreamEvent("INFO", message, callerMemberName, callerFilePath, callerLineNumber));
    }

    public static void WriteDebug(this IInfoStream self, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
    {
        self.WriteEvent(new InfoStreamEvent("DEBUG", message, callerMemberName, callerFilePath, callerLineNumber));
    }
}