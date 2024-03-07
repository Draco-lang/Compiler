namespace Draco.Compiler.Internal.Solver.Utilities;

/// <summary>
/// Represents the comparison result of two <see cref="CallScore"/>s.
/// </summary>
internal enum CallScoreComparison
{
    /// <summary>
    /// The relationship could not be determined, because the scores are not well-defined.
    /// </summary>
    Undetermined,

    /// <summary>
    /// The first score vector dominates the second.
    /// </summary>
    FirstDominates,

    /// <summary>
    /// The second score vector dominates the first.
    /// </summary>
    SecondDominates,

    /// <summary>
    /// There is mutual non-dominance.
    /// </summary>
    NoDominance,

    /// <summary>
    /// The two scores are equal.
    /// </summary>
    Equal,
}
