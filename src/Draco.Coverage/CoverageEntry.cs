namespace Draco.Coverage;

/// <summary>
/// A coverage entry.
/// </summary>
/// <param name="SequencePoint">The sequence point.</param>
/// <param name="Hits">The number of hits.</param>
public readonly record struct CoverageEntry(SequencePoint SequencePoint, int Hits);
