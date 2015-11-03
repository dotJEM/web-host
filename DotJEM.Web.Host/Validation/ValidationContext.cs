using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DotJEM.Web.Host.Providers.Pipeline;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public class ValidationContext : IValidationContext
    {
        public ValidationContext(JObject entityBeingValidated, JObject previousObject, PipelineContext context, HttpVerbs requestType)
        {
            Entity = entityBeingValidated;
            PreviousEntity = previousObject;
            HttpRequestType = requestType;
            Context = context;
        }

        public PipelineContext Context { get; private set; }

        public JObject Entity { get; private set; }

        public JObject PreviousEntity { get; private set; }

        public HttpVerbs HttpRequestType { get; private set; }
    }
}
