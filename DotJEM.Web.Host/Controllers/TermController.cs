using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotJEM.Json.Index;

namespace DotJEM.Web.Host.Controllers
{
    //TODO: This can be a part of a SchemaController instead.
    public class TermController : WebHostApiController
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
                return BadRequest("No field was specified");
         
            return index.Terms(field);
        }
    }
}