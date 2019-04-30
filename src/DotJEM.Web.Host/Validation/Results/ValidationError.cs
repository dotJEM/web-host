using System;
using DotJEM.Json.Index.Schema;

namespace DotJEM.Web.Host.Validation.Results
{
    [Obsolete]
    public class ValidationError
    {
        private readonly string format;
        private readonly object[] args;

        public ValidationError(string format, object[] args)
        {
            this.format = format;
            this.args = args;
        }

        public string Format(JPath field)
        {
            return string.Format(format.Replace("@(field)", "'" + field.ToString(" ") + "'"), args);
        }
    }
}