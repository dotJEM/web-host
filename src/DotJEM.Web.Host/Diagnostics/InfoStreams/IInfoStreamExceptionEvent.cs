using System;

namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public interface IInfoStreamExceptionEvent : IInfoStreamEvent
{
    Exception Exception { get; }
}