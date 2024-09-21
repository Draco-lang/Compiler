namespace Draco.Coverage;

/// <summary>
/// A coverage entry.
/// </summary>
public readonly struct CoverageEntry(SequencePoint sequencePoint, int hits)
{
    /// <summary>
    /// The sequence point of the coverage entry.
    /// </summary>
    public readonly SequencePoint SequencePoint = sequencePoint;

    /// <summary>
    /// The number of hits.
    /// </summary>
    public readonly int Hits = hits;
}
