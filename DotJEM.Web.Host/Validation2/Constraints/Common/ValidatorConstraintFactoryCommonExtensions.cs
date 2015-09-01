using DotJEM.Web.Host.Validation2.Constraints.String.Length;
using DotJEM.Web.Host.Validation2.Factories;

namespace DotJEM.Web.Host.Validation2.Constraints.Common
{
    public static class GuardConstraintFactoryCommonExtensions
    {
        public static JsonConstraint Defined(this IBeConstraintFactory self)
        {
            return new IsDefinedJsonConstraint();
        }
    }
}