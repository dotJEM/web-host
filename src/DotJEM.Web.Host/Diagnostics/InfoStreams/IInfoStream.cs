using System;

namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public interface IInfoStream : IObservable<IInfoStreamEvent>
{
    void Forward(IInfoStream infoStream);
    void WriteEvent(IInfoStreamEvent evt);
}