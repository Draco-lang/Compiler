using System;
using System.Collections.Generic;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Generic binary search implementation.
/// </summary>
internal static class BinarySearch
{
    /// <summary>
    /// Performs a binary search.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TKey">The searched key type.</typeparam>
    /// <param name="items">The span of items to search in.</param>
    /// <param name="searchedKey">The searched key.</param>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="keyComparer">The key comparer.</param>
    /// <returns>A tuple of the index that tells where the item can be inserted to maintain ordered-ness
    /// and a boolean indicating if it was an exact match.</returns>
    public static (int Index, bool ExactMatch) Search<TItem, TKey>(
        ReadOnlySpan<TItem> items,
        TKey searchedKey,
        Func<TItem, TKey> keySelector,
        IComparer<TKey>? keyComparer = null)
    {
        keyComparer ??= Comparer<TKey>.Default;

        var size = items.Length;
        var left = 0;
        var right = size;

        while (left < right)
        {
            var mid = left + size / 2;
            var cmp = keyComparer.Compare(searchedKey, keySelector(items[mid]));

            if (cmp == 0) return (mid, true);

            if (cmp > 0) left = mid + 1;
            else right = mid;

            size = right - left;
        }

        return (left, false);
    }
}
