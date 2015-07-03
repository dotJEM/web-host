using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation
{
    public interface IValidationContext
    {

        JObject Entity { get; }
        JObject PreviousEntity { get; }

        HttpVerbs HttpRequestType { get; }

    }
}
