using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Web.Host.Util;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T elm in source)
            action(elm);
    }
    public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> source, Action<TKey, TValue> action)
    {
        source.ForEach(kv => action(kv.Key, kv.Value));
    }

    public static void ForEach<T, TIgnore>(this IEnumerable<T> source, Func<T, TIgnore> action)
    {
        foreach (T elm in source)
            action(elm);
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int i = 0;
        foreach (T elm in source)
            action(elm, i++);
    }

    public static T FirstOr<T>(this IEnumerable<T> source, T @default)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        IList<T> list = source as IList<T>;
        if (list != null && list.Count > 0)
            return list.First();

        using (IEnumerator<T> enumerator = source.GetEnumerator())
        {
            if (enumerator.MoveNext())
                return enumerator.Current;
        }

        return @default;
    }

    public static T SingleOr<T>(this IEnumerable<T> source, T @default)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        IList<T> list = source as IList<T>;
        if (list != null && list.Count < 2)
            return list.FirstOr(@default);

        using (IEnumerator<T> enumerator = source.GetEnumerator())
        {
            if (!enumerator.MoveNext()) return @default;
            T current = enumerator.Current;
            if (!enumerator.MoveNext()) return current;
        }

        throw new InvalidOperationException("Source contained more than one element.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (size < 1) throw new ArgumentOutOfRangeException("size", "The chunkSize parameter must be a positive value.");
        return InternalPartitions(source, size);
    }

    private static IEnumerable<IEnumerable<T>> InternalPartitions<T>(this IEnumerable<T> source, int size)
    {
        using (IEnumerator<T> enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                yield return InternalPartition(enumerator, size);
            }
        }
    }

    private static IEnumerable<T> InternalPartition<T>(IEnumerator<T> enumerator, int size)
    {
        int i = 0;
        do { 
            yield return enumerator.Current;
        } while (++i < size && enumerator.MoveNext());
    }
}