using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Json.Index;

namespace DotJEM.Web.Host.Controllers
{
    public class TermController : ApiController
    {
        private readonly IStorageIndex index;

        public TermController(IStorageIndex index)
        {
            this.index = index;
        }

        [HttpGet]
        public dynamic Get([FromUri]string field)
        {
            if (string.IsNullOrWhiteSpace(field) && string.IsNullOrWhiteSpace(field))
                Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a field.");
         
            return index.Terms(field);
        }
    }
}