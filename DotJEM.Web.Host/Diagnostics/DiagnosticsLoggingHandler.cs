using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics
{
    public class DiagnosticsLoggingHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            long timing = Stopwatch.GetTimestamp();
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            timing = Stopwatch.GetTimestamp() - timing;
            
            //TODO: Aquire all data, then log asyncrinously instead here (removing await)...


            await Task.Run(() => Log(request.Method, request.RequestUri, response, timing), cancellationToken);
            return response;
        }

        private void Log(HttpMethod requestMethod, Uri requestUri, HttpResponseMessage response, long timing)
        {
            Debug.WriteLine("{0}: {1}\n - {2}\n - {3}\n - {4}\n - {5}\n - {6}ms\n - {7}\n - {8}\n - {9}",
                requestMethod,
                requestUri,
                DateTime.UtcNow,
                ClaimsPrincipal.Current.Identity.Name,
                Thread.CurrentPrincipal.Identity.Name,
                (int)response.StatusCode,
                timing,
                Environment.MachineName,
                Assembly.GetExecutingAssembly().GetName().Version,
                Thread.CurrentThread.ManagedThreadId);
        }

    }
}
