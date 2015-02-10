using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Web.Host.Configuration;
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

        IStorageIndexManager Watch(params string[] areas);

        Task QueueUpdate(JObject entity);
    }

    public class StorageIndexManager : IStorageIndexManager
    {
        private readonly IStorageIndex index;
        private readonly IStorageContext storage;
        private readonly IWebHostConfiguration config;

        private Scheduler callback;
        private Dictionary<string, IStorageAreaLog> logs = new Dictionary<string, IStorageAreaLog>();

        public StorageIndexManager(IStorageIndex index, IStorageContext storage, IWebHostConfiguration config)
        {
            this.index = index;
            this.storage = storage;
            this.config = config;
        }

        public void Start()
        {
             callback = new Scheduler(signaled => UpdateIndex(), TimeSpan.FromSeconds(60));
        }

        public void Stop()
        {
            callback.Dispose();
        }

        public IStorageIndexManager Watch(params string[] areas)
        {
            foreach (string area in areas)
                logs[area] = storage.Area(area).Log;
            return this;
        }

        private void UpdateIndex(params JObject[] entities)
        {
            logs.Values
                .AsParallel()
                .Select(log => log.Get())
                
                .Select(changes =>
                {
                    index.WriteAll(changes.Creates);
                    index.WriteAll(changes.Updates);
                    index.DeleteAll(changes.Deletes);
                    return changes.Token;
                })
                .Max();

            //watchedAreas
            //    .Values
            //    .AsParallel()
            //    .Select(log => { return log; })
            //    .SelectMany()


            index.WriteAll(entities);
        }

        public Task QueueUpdate(JObject entity)
        {
            //Note: This will cause the callback to get called right away...
            callback.Signal();

            //UpdateIndex(entity);


            //TODO: This is cheating atm... 
            return Task.Factory.StartNew(() => { });
        }

        public void Update()
        {
            
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
}
