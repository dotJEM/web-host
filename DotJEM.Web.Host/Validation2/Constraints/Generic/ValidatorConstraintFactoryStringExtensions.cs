using System.Collections.Generic;
using DotJEM.Web.Host.Validation2.Factories;

namespace DotJEM.Web.Host.Validation2.Constraints.Generic
{
    public static class ConstraintFactoryGenericExtensions
    {
        public static JsonConstraint In<T>(this IBeConstraintFactory self, params T[] args)
        {
            return new InConstraint<T>(args);
        }
        public static JsonConstraint In<T>(this IBeConstraintFactory self, IEnumerable<T> values)
        {
            return new InConstraint<T>(values);
        }

    }
}