using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface ITermService
    {
        JObject Get(string contentType, string field);

        //// TERM CONTROLLER
        //[HttpGet]
        //public dynamic Get([FromUri]string field)
        //{
        //    if (string.IsNullOrWhiteSpace(field) && string.IsNullOrWhiteSpace(field))
        //        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a field.");

        //    return index.Terms(field);
        //}
    }

    public class TermService : ITermService
    {
        public JObject Get(string contentType, string field)
        {
            throw new System.NotImplementedException();
        }
    }
}









