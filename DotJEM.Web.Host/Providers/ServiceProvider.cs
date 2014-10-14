using System;
using System.Collections.Generic;
using System.Web.Http;

namespace DotJEM.Web.Host.Providers
{
    public interface IServiceProvider<out TService>
    {
        TService Create(string areaName, ApiController controller);
        bool Release(string areaName);
    }

    public abstract class ServiceProvider<TService> : IServiceProvider<TService>
    {
        private readonly Func<string, TService> factory; 
        private readonly Dictionary<string, TService> services = new Dictionary<string, TService>();

        protected ServiceProvider(Func<string, TService> factory)
        {
            this.factory = factory;
        }

        public TService Create(string areaName, ApiController controller)
        {
            TService service = GetOrCreateService(areaName);
            //service.Controller = controller;
            return service;
        }

        private TService GetOrCreateService(string areaName)
        {
            return !services.ContainsKey(areaName) ? (services[areaName] = factory(areaName)) : services[areaName];
        }

        public virtual bool Release(string areaName)
        {
            return services.Remove(areaName);
        }
    }
}