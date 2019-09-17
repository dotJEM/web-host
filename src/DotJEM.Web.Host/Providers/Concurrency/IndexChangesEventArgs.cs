using System;
using System.Collections.Generic;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class IndexChangesEventArgs : EventArgs
    {
        public IDictionary<string, IStorageChangeCollection> Changes { get; }

        public IndexChangesEventArgs(IDictionary<string, IStorageChangeCollection> changes)
        {
            Changes = changes;
        }
    }
}