using System;
using System.Collections.Immutable;

namespace Draco.Coverage;

/// <summary>
/// The result of a coverage run.
/// </summary>
public readonly struct CoverageResult
{
    /// <summary>
    /// An empty coverage result.
    /// </summary>
    public static CoverageResult Empty { get; } = new([], []);

    /// <summary>
    /// The sequence points of the weaved assembly.
    /// </summary>
    public ImmutableArray<SequencePoint> SequencePoints { get; }

    /// <summary>
    /// The hit counts of each sequence point.
    /// </summary>
    public ImmutableArray<int> Hits { get; }

    public CoverageResult(ImmutableArray<SequencePoint> sequencePoints, ImmutableArray<int> hits)
    {
        if (sequencePoints.Length != hits.Length)
        {
            throw new ArgumentException("the sequence points and hits arrays must have the same length");
        }

        this.SequencePoints = sequencePoints;
        this.Hits = hits;
    }
}
