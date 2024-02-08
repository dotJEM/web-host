using System;
using DotJEM.Json.Storage.Adapter;

namespace DotJEM.Web.Host.Providers.Data.Storage;

public interface IStorageHistoryCleaner
{
    void Execute();
}

public class StorageHistoryCleaner : IStorageHistoryCleaner
{
    private readonly TimeSpan maxAge;
    private readonly Lazy<IStorageAreaHistory> serviceProvider;

    private IStorageAreaHistory History => serviceProvider.Value;

    public StorageHistoryCleaner(Lazy<IStorageAreaHistory> serviceProvider, TimeSpan maxAge)
    {
        this.serviceProvider = serviceProvider;
        this.maxAge = maxAge;
    }

    public void Execute()
    {
        History.Delete(maxAge);
    }
}