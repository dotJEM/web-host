using System;
using System.Collections.Generic;

namespace DotJEM.Web.Host.Diagnostics.InfoStreams;

public class DefaultInfoStream<TOwner> : IInfoStream
{
    private readonly Dictionary<Guid, IObserver<IInfoStreamEvent>> subscribers = new();

    public void Forward(IInfoStream infoStream)
    {
        Subscribe(new ForwardingSubscriber(infoStream));
    }

    public void WriteEvent(IInfoStreamEvent evt)
    {
        foreach (IObserver<IInfoStreamEvent> observer in subscribers.Values)
        {
            try
            {
                observer.OnNext(evt);
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }
    }

    public IDisposable Subscribe(IObserver<IInfoStreamEvent> observer)
    {
        Guid id = Guid.NewGuid();
        subscribers.Add(id, observer);
        return new Subscription(subscribers, id);
    }

    private class ForwardingSubscriber : IObserver<IInfoStreamEvent>
    {
        private readonly IInfoStream infoStream;

        public ForwardingSubscriber(IInfoStream infoStream)
        {
            this.infoStream = infoStream;
        }

        public void OnNext(IInfoStreamEvent value)
        {
            infoStream.WriteEvent(value);
        }

        public void OnError(Exception error)
        {
            //TODO?
        }

        public void OnCompleted()
        {
            //Info Streams does not complete.
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Dictionary<Guid, IObserver<IInfoStreamEvent>> subscribers;
        private readonly Guid id;

        public Subscription(Dictionary<Guid, IObserver<IInfoStreamEvent>> subscribers, Guid id)
        {
            this.subscribers = subscribers;
            this.id = id;
        }

        public void Dispose()
        {
            subscribers.Remove(id);
        }
    }
}