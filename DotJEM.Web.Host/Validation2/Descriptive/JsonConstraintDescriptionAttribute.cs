using System;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Validation2.Constraints.Descriptive
{
    public class JsonConstraintDescriptionAttribute : Attribute
    {
        public string Format { get; private set; }

        public JsonConstraintDescriptionAttribute(string format)
        {
            Format = format;
        }
    }
}