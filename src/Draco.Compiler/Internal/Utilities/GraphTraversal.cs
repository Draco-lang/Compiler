using System;
using System.Collections.Generic;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Implements generic graph traversal algorithms.
/// </summary>
internal sealed class GraphTraversal
{
    /// <summary>
    /// Performs a depth-first search.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="start">The starting vertex.</param>
    /// <param name="getNeighbors">A function that retrieves the neighbors of a vertex in any order.</param>
    /// <param name="comparer">The vertex comparer.</param>
    /// <returns>The vertices, starting from <paramref name="start"/>, in some depth-first order.</returns>
    public static IEnumerable<TVertex> DepthFirst<TVertex>(
        TVertex start,
        Func<TVertex, IEnumerable<TVertex>> getNeighbors,
        IComparer<TVertex>? comparer = null)
    {
        comparer ??= Comparer<TVertex>.Default;
        var labeled = new HashSet<TVertex>();
        var stk = new Stack<TVertex>();
        stk.Push(start);
        while (stk.TryPop(out var v))
        {
            if (!labeled.Add(v)) continue;
            yield return v;
            foreach (var w in getNeighbors(v)) stk.Push(w);
        }
    }
}
