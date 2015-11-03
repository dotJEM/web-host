using DotJEM.Web.Host.Validation2.Constraints.Descriptive;
using DotJEM.Web.Host.Validation2.Context;
using Lucene.Net.Analysis;

namespace DotJEM.Web.Host.Validation2.Constraints.String.Length
{
    [JsonConstraintDescription("length must be '{length}'.")]
    public class ExactStringLengthJsonConstraint : TypedJsonConstraint<string>
    {
        private readonly int length;

        public ExactStringLengthJsonConstraint(int length)
        {
            this.length = length;
        }

        protected override bool Matches(IJsonValidationContext context, string value)
        {
            return value.Length == length;
        }
    }
}