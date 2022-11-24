using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Web.Host.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    //TODO: Not really a stream, but we need some info for now, then we can make it into a true info stream later.
    public class StorageIndexManagerInfoStream : IStorageIndexManagerInfoStream
    {
        private readonly ConcurrentDictionary<string, AreaInfo> areas = new ConcurrentDictionary<string, AreaInfo>();

        public void Track(string area, int creates, int updates, int deletes, int faults)
        {
            AreaInfo info = areas.GetOrAdd(area, s => new AreaInfo(s));
            info.Track(creates, updates, deletes, faults);
        }

        public void Record(string area, IList<FaultyChange> faults)
        {
            AreaInfo info = areas.GetOrAdd(area, s => new AreaInfo(s));
            info.Record(faults);
        }

        public virtual void Publish(IStorageChangeCollection changes)
        {
        }

        public JObject ToJObject()
        {
            JObject json = new JObject();
            long creates = 0, updates = 0, deletes = 0, faults = 0;
            //NOTE: This places them at top which is nice for human readability, machines don't care.
            json["creates"] = creates;
            json["updates"] = updates;
            json["deletes"] = deletes;
            json["faults"] = faults;
            foreach (AreaInfo area in areas.Values)
            {
                creates += area.Creates;
                updates += area.Updates;
                deletes += area.Deletes;
                faults += area.Faults;

                json[area.Area] = area.ToJObject();
            }
            json["creates"] = creates;
            json["updates"] = updates;
            json["deletes"] = deletes;
            json["faults"] = faults;
            return json;
        }

        private class AreaInfo
        {
            private long creates = 0, updates = 0, deletes = 0, faults = 0;
            private readonly ConcurrentBag<FaultyChange> faultyChanges = new ConcurrentBag<FaultyChange>();

            public string Area { get; }

            public long Creates => creates;
            public long Updates => updates;
            public long Deletes => deletes;
            public long Faults => faults;
            public FaultyChange[] FaultyChanges => faultyChanges.ToArray();

            public AreaInfo(string area)
            {
                Area = area;
            }

            public void Track(int creates, int updates, int deletes, int faults)
            {
                Interlocked.Add(ref this.creates, creates);
                Interlocked.Add(ref this.updates, updates);
                Interlocked.Add(ref this.deletes, deletes);
                Interlocked.Add(ref this.faults, faults);
            }

            public void Record(IList<FaultyChange> faults)
            {
                faults.ForEach(faultyChanges.Add);
            }

            public JObject ToJObject()
            {
                JObject json = new JObject();
                json["creates"] = creates;
                json["updates"] = updates;
                json["deletes"] = deletes;
                json["faults"] = faults;
                if (faultyChanges.Any())
                    json["faultyChanges"] = JArray.FromObject(FaultyChanges.Select(c => c.CreateEntity()));
                return json;
            }
        }
    }
}