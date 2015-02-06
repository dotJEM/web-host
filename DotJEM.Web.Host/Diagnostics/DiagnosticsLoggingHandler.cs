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
            
            Task.Run(() => Log(request, response, timing), cancellationToken);
            return response;
        }

        private void Log(HttpRequestMessage request, HttpResponseMessage response, long ms)
        {
            //request properties

            //response properties
           
            //server &, application properties

            Debug.WriteLine("{0}: {1}\n - {2}\n - {3}\n - {4}\n - {5}\n - {6}ms\n - {7}\n - {8}\n - {9}",
                request.Method,
                request.RequestUri,
                DateTime.UtcNow,
                ClaimsPrincipal.Current.Identity.Name,
                Thread.CurrentPrincipal.Identity.Name,
                (int)response.StatusCode,
                ms,
                Environment.MachineName,
                Assembly.GetExecutingAssembly().GetName().Version,
                Thread.CurrentThread.ManagedThreadId);

            //todo: log to your favorite persistence store
        }
    }
}
