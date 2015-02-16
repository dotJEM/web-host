using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Configuration;
using DotJEM.Web.Host.Configuration.Elements;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IStorageIndexManager>().ImplementedBy<StorageIndexManager>().LifestyleSingleton());
        }
    }

    public interface IStorageIndexManager
    {
        void Start();
        void Stop();
        void QueueUpdate();
    }

    public class StorageIndexManager : IStorageIndexManager
    {
        private readonly IStorageIndex index;
        private readonly object padlock = new object();

        private Scheduler callback;
        private readonly Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();
        private readonly TimeSpan interval;
        private readonly string cachePath;
        private bool initialized;

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration configuration)
        {
            this.index = index;
            interval = TimeSpan.FromSeconds(configuration.Index.Watch.Interval);
            foreach (WatchElement watch in configuration.Index.Watch.Items)
                logs[watch.Area] = storage.Area(watch.Area).Log;

            //TODO: Use the below to store a index pointer.
            if (!string.IsNullOrEmpty(configuration.Index.CacheLocation))
                cachePath = Path.Combine(HostingEnvironment.MapPath(configuration.Index.CacheLocation), "tracker");
        }

        public void Start()
        {
            UpdateIndex();
            callback = new Scheduler(signaled => UpdateIndex(), interval);
        }

        public void Stop()
        {
            callback.Dispose();
        }

        private void UpdateIndex()
        {
            IEnumerable<Tuple<string, IStorageChanges>> tuples;
            if (!initialized)
            {
                Dictionary<string, long> tracker = InitializeFromTracker();
                tuples = logs.Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get(GetTracker(tracker, log.Key))));
                initialized = true;
            }
            else
            {
                tuples = logs.Select(log => new Tuple<string, IStorageChanges>(log.Key, log.Value.Get()));
            }

            UpdateTracker(tuples.Select(Selector)
            .Aggregate(new Dictionary<string, long>(), (map, next) =>
            {
                map[next.Item1] = next.Item2;
                return map;
            }));
        }

        private static long GetTracker(Dictionary<string, long> changes, string key)
        {
            long value;
            if (changes.TryGetValue(key, out value))
                return value;
            return -1;
        }

        private void UpdateTracker(Dictionary<string, long> changes)
        {
            lock (padlock)
            {
                if (!string.IsNullOrEmpty(cachePath))
                {
                    new SimpleDictionaryWriter().Write(cachePath, changes);
                }
            }
        }

        private Dictionary<string, long> InitializeFromTracker()
        {
            lock (padlock)
            {
                return new SimpleDictionaryWriter().Read(cachePath);
            }
        }

        private Tuple<string, long> Selector(Tuple<string, IStorageChanges> tuple)
        {
            var changes = tuple.Item2;
            index
                .WriteAll(changes.Created)
                .WriteAll(changes.Updated)
                .DeleteAll(changes.Deleted);
            return new Tuple<string, long>(tuple.Item1, changes.Token);
        }

        public void QueueUpdate()
        {
            //Note: This will cause the callback to get called right away...
            callback.Signal();
        }

    }

    public class Scheduler : IDisposable
    {
        private readonly Action<bool> callback;
        private readonly TimeSpan period;
        private readonly AutoResetEvent handle = new AutoResetEvent(false);
        private bool disposed = false;

        public Scheduler(Action<bool> callback, TimeSpan period)
        {
            this.callback = callback;
            this.period = period;
            Next();
        }

        private void Next()
        {
            ThreadPool.RegisterWaitForSingleObject(handle, ExecuteCallback, null, period, true);
        }

        private void ExecuteCallback(object state, bool timedout)
        {
            if (disposed)
                return;

            callback(!timedout);
            Next();
        }

        public void Dispose()
        {
            disposed = true;
            Signal();
        }

        public void Signal()
        {
            handle.Set();
        }
    }


    public class SimpleDictionaryWriter
    {
        public void Write(string file, Dictionary<string, long> values)
        {
            byte[] buffer = new byte[1024 * 16];
            int offset = values.Aggregate(0, (current, change) => WriteKeyValueToBuffer(change, buffer, current));
            byte[] outputBuffer = new byte[offset];
            Buffer.BlockCopy(buffer, 0, outputBuffer, 0, offset);
            File.WriteAllBytes(file, outputBuffer);
        }

        private int WriteKeyValueToBuffer(KeyValuePair<string, long> kvp, byte[] buffer, int offset)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(kvp.Key);
            byte[] token = BitConverter.GetBytes(kvp.Value);

            buffer[offset++] = (byte)bytes.Length;
            Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
            Buffer.BlockCopy(token, 0, buffer, offset + bytes.Length, token.Length);
            return offset + bytes.Length + 8;
        }

        private int ReadKeyValueFromBuffer(byte[] buffer, Dictionary<string, long> map, int offset)
        {
            byte[] bytes = new byte[buffer[offset++]];

            Buffer.BlockCopy(buffer, offset, bytes, 0, bytes.Length);
            offset += bytes.Length;
            string key = Encoding.UTF8.GetString(bytes);
            map[key] = BitConverter.ToInt64(buffer, offset);
            return offset + 8;
        }

        public Dictionary<string, long> Read(string file)
        {
            if(!File.Exists(file))
                return new Dictionary<string, long>();
            byte[] inputBuffer = File.ReadAllBytes(file);
            Dictionary<string, long> map = new Dictionary<string, long>();
            int offset = 0;
            while (offset < inputBuffer.Length)
            {
                offset = ReadKeyValueFromBuffer(inputBuffer, map, offset);
            }
            return map;
        }
    }
}
