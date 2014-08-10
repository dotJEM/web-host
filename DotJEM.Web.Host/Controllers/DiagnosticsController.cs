using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Controllers
{
    public abstract class DiagnosticsController : StorageController
    {
        protected DiagnosticsController(IStorageContext storage, IStorageIndex index, string storageArea)
            : base(storage, index, storageArea)
        {
        }

        [HttpPost]
        public override dynamic Post([FromUri]string contentType, [FromBody]JObject entity)
        {
            dynamic server = entity["server"] = new JObject();
            server.time = DateTime.Now;

            dynamic client = entity["client"] = new JObject();
            client.ipAddress = Request.GetClientIpAddress();
            client.headers = Request.TransformHeaders();

            return base.Post(contentType, entity);
        }

        public override dynamic Put(Guid id, string contentType, JObject entity)
        {
            return Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Update of diagnostic messages is not allowed.");
        }
    }

    public static class HttpRequestMessageExtensions
    {
        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        private const string OwinContext = "MS_OwinContext";

        public static JObject TransformHeaders(this HttpRequestMessage request)
        {
            JObject json = new JObject();
            foreach (var header in request.Headers)
            {
                json[header.Key] = JArray.FromObject(header.Value);
            }
            return json;
        }

        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            // Web-hosting. Needs reference to System.Web.dll
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            // Self-hosting. Needs reference to System.ServiceModel.dll. 
            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            // Self-hosting using Owin. Needs reference to Microsoft.Owin.dll. 
            if (request.Properties.ContainsKey(OwinContext))
            {
                dynamic owinContext = request.Properties[OwinContext];
                if (owinContext != null)
                {
                    return owinContext.Request.RemoteIpAddress;
                }
            }

            return null;
        }


    }
}