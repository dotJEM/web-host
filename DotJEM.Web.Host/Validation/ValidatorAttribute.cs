using System;

namespace DotJEM.Web.Host.Validation
{
    public class ValidatorAttribute : Attribute
    {
        public string ContentType { get; set; }

        public ValidatorAttribute(string contentType)
        {
            ContentType = contentType;
        }
    }
}