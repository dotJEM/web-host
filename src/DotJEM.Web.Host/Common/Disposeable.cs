using System;

namespace DotJEM.Web.Host.Common;

public abstract class Disposeable : IDisposable
{
    protected volatile bool Disposed;

    protected virtual void Dispose(bool disposing)
    {
        Disposed = true;
    }

    ~Disposeable()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}