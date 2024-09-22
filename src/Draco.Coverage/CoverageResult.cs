using System.Collections.Immutable;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public readonly struct CoverageResult(ImmutableArray<int> hits)
{
    /// <summary>
    /// An empty coverage result.
    /// </summary>
    public static CoverageResult Empty { get; } = new([]);

    /// <summary>
    /// The hit counts of each sequence point.
    /// </summary>
    public ImmutableArray<int> Hits { get; } = hits;
}
