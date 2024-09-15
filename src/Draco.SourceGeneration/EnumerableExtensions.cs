using System.Collections.Generic;

namespace Draco.SourceGeneration;

/// <summary>
/// Polyfill for ns2.0 things.
/// </summary>
internal static class EnumerableExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items, EqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var result = new HashSet<T>(comparer);
        foreach (var item in items) result.Add(item);
        return result;
    }
}
