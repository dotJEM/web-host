using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Castle.Windsor;

namespace DotJEM.Web.Host.Providers
{
    public interface IServiceProvider<out TService>
    {
        TService Create(string areaName);
        bool Release(string areaName);
    }

    public abstract class ServiceProvider<TService> : IServiceProvider<TService>
    {
        private readonly Func<string, TService> factory;
        //private readonly Dictionary<string, TService> services = new Dictionary<string, TService>();
        private readonly ConcurrentDictionary<string, TService> services = new ConcurrentDictionary<string, TService>();

        protected ServiceProvider(Func<string, TService> factory)
        {
            this.factory = factory;
        }

        public TService Create(string areaName)
        {
            TService service = GetOrCreateService(areaName);


            //service.Controller = controller;
            return service;
        }

        private TService GetOrCreateService(string areaName)
        {
            try
            {
                return services.GetOrAdd(areaName, factory);

                //return !services.ContainsKey(areaName) ? (services[areaName] = factory(areaName)) : services[areaName];
            }
            catch (NullReferenceException ex)
            {
                string message = areaName != null ? "Area name was: " + areaName : "Area name was NULL: ";
                throw new NullReferenceException(message, ex);
            }

        }

        public virtual bool Release(string areaName)
        {
            TService removed;
            return services.TryRemove(areaName, out removed);
        }
    }
}