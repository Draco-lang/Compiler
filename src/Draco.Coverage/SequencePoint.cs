namespace Draco.Coverage;

/// <summary>
/// A sequence point in a source file.
/// </summary>
public readonly record struct SequencePoint(
    string FileName,
    int Offset,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
