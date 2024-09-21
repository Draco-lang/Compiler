using System.Collections.Immutable;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public sealed class CoverageResult(ImmutableArray<CoverageEntry> entires)
{
    /// <summary>
    /// An empty coverage result.
    /// </summary>
    public static CoverageResult Empty { get; } = new([]);

    /// <summary>
    /// The coverage entries.
    /// </summary>
    public ImmutableArray<CoverageEntry> Entires { get; } = entires;
}