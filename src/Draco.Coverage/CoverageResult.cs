using System.Collections.Immutable;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public readonly struct CoverageResult(ImmutableArray<int> hits)
{
    /// <summary>
    /// Creates a new coverage result from the given shared memory.
    /// </summary>
    /// <param name="hits">The shared memory containing the hit counts.</param>
    /// <returns>The coverage result.</returns>
    public static CoverageResult FromSharedMemory(SharedMemory<int> hits) => new([.. hits.Span]);

    /// <summary>
    /// An empty coverage result.
    /// </summary>
    public static CoverageResult Empty { get; } = new([]);

    /// <summary>
    /// The hit counts of each sequence point.
    /// </summary>
    public ImmutableArray<int> Hits { get; } = hits;
}
