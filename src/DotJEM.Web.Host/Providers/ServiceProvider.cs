using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Castle.Windsor;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;

namespace DotJEM.Web.Host.Providers
{
    public interface IServiceProvider<out TService>
    {
        TService Create(string areaName);
        bool Release(string areaName);
    }

    public abstract class ServiceProvider<TService> : IServiceProvider<TService> where TService : class
    {
        private readonly Func<string, TService> factory;
        private readonly ILogger logger;
        private readonly IPerformanceLogAspectSignatureCache cache;
        private readonly ConcurrentDictionary<string, TService> services = new();

        protected ServiceProvider(Func<string, TService> factory, ILogger logger, IPerformanceLogAspectSignatureCache cache)
        {
            this.factory = factory;
            this.logger = logger;
            this.cache = cache;
        }

        public TService Create(string areaName) => GetOrCreateService(areaName);

        private TService GetOrCreateService(string areaName)
        {
            if (areaName == null) throw new ArgumentNullException(nameof(areaName));
            try
            {
                return services.GetOrAdd(areaName, CreateService);
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException("Area name was: " + areaName, ex);
            }

            TService CreateService(string area) {
                TService service = factory(area);
                return logger.IsEnabled() 
                    ? new ProxyGenerator().CreateInterfaceProxyWithTarget(service, new PerformanceLogAspect(logger, cache))
                    : service;
            }
        }

        public virtual bool Release(string areaName)
        {
            return services.TryRemove(areaName, out TService _);
        }
    }
}