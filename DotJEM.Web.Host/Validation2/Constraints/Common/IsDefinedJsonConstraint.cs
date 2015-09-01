using System;
using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.Common
{
    [JsonConstraintDescription("String must be equal to '{value}' ({comparison}).")]
    public class IsDefinedJsonConstraint : JsonConstraint
    {
        public override bool Matches(IJsonValidationContext context, JToken token)
        {
            return token != null && token.Type != JTokenType.Null && token.Type != JTokenType.Undefined;
        }
    }
}