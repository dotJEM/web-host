using System.Web.Hosting;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DotJEM.Web.Host.Abstractions
{
    public class AbstractionsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IHostingEnvironment>().ImplementedBy<HostingEnvironmentAccessor>().LifestyleSingleton());
        }
    }

    public interface IHostingEnvironment
    {
        void RegisterObject(IRegisteredObject obj);
        void UnregisterObject(IRegisteredObject obj);
    }

    public class HostingEnvironmentAccessor : IHostingEnvironment
    {

        public void RegisterObject(IRegisteredObject obj) => HostingEnvironment.RegisterObject(obj);
        public void UnregisterObject(IRegisteredObject obj) => HostingEnvironment.UnregisterObject(obj);
        

    }
}
