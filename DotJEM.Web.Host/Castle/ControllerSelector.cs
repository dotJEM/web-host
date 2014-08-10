using System.Collections.Concurrent;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;

namespace DotJEM.Web.Host.Castle
{
    public class ControllerSelector : DefaultHttpControllerSelector
    {
        private readonly HttpConfiguration configuration;
        private readonly ConcurrentDictionary<string, HttpControllerDescriptor> controllers = new ConcurrentDictionary<string, HttpControllerDescriptor>();

        public ControllerSelector(HttpConfiguration configuration)
            : base(configuration)
        {
            this.configuration = configuration;
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            IHttpRouteData routeDate = request.GetRouteData();
            IWebRoute ewroute = (IWebRoute)routeDate.Route;
            if (ewroute == null)
                return base.SelectController(request);

            return controllers.GetOrAdd(
                ewroute.ControllerName,
                key => new HttpControllerDescriptor(configuration, key, ewroute.ControllerType));
        }
    }
}
