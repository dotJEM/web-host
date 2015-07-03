using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public class ValidationContext : IValidationContext
    {
        public ValidationContext(JObject entityBeingValidated, JObject previousObject, HttpVerbs requestType)
        {
            Entity = entityBeingValidated;
            PreviousEntity = previousObject;
            HttpRequestType = requestType;
        }

        public JObject Entity { get; private set; }

        public JObject PreviousEntity { get; private set; }

        public HttpVerbs HttpRequestType { get; private set; }
    }
}
