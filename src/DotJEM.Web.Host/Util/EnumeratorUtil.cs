using System;
using System.Collections.Generic;

namespace DotJEM.Web.Host.Util
{
    public static class EnumeratorUtil
    {
        public static IEnumerable<T> Generate<T>(Func<T> generatorFunc, Func<T, bool> breakFunc = null)
        {
            T def = default(T);
            breakFunc = breakFunc ?? (v => Equals(v, def));
            while (true)
            {
                T value = generatorFunc();
                if (breakFunc(value)) 
                    yield break;

                yield return value;
            }
        } 
    }
}