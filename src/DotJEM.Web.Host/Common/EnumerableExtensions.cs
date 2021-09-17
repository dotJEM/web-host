using System.Collections.Generic;

namespace DotJEM.Web.Host.Common
{
    public static class EnumerableExtensions
    {
        public static void Enumerate<T>(this IEnumerable<T> source)
        {
            // ReSharper disable EmptyEmbeddedStatement
            foreach (T _ in source);
            // ReSharper restore EmptyEmbeddedStatement
        }
    }
}