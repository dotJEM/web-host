using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;

namespace Demo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configuration.MessageHandlers.Add(new ApiKeyAuthorizationHandler());

            new DemoHost(GlobalConfiguration.Configuration).Start();
        }
    }

    public class ApiKeyAuthorizationHandler : DelegatingHandler
    {
        private ApiKeyAuthenticationService service = new ApiKeyAuthenticationService();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-ApiKey", out IEnumerable<string> values))
            {
                string apiKey = values.SingleOrDefault();
                string name = service.Verify(apiKey);
                if (name != null)
                {
                    Claim claim = new Claim(ClaimTypes.Name, name);
                    ClaimsIdentity identity = new ClaimsIdentity(new[] { claim }, "ApiKey");
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    HttpContext.Current.User = principal;
                }
            }
            else
            {
                string apiKey = request
                    .GetQueryNameValuePairs()
                    .SingleOrDefault(pair => pair.Key.Equals("apikey", StringComparison.InvariantCultureIgnoreCase)).Value;
                string name = service.Verify(apiKey);
                if (name != null)
                {
                    Claim claim = new Claim(ClaimTypes.Name, name);
                    ClaimsIdentity identity = new ClaimsIdentity(new[] { claim }, "ApiKey");
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    HttpContext.Current.User = principal;
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    public class ApiKeyAuthenticationService
    {
        public string Verify(string apiKey)
        {
            if (apiKey == "44de02602a6c4a14b1b2fff6829b0ba4")
            {
                return "James";
            }
            return null;
        }
    }
}
