namespace Draco.Coverage;

/// <summary>
/// A sequence point in a source file.
/// </summary>
public readonly struct SequencePoint(
    string fileName,
    int offset,
    int startLine,
    int startColumn,
    int endLine,
    int endColumn)
{
    /// <summary>
    /// The file name where the sequence point is located.
    /// </summary>
    public readonly string FileName = fileName;

    /// <summary>
    /// The IL offset of the sequence point.
    /// </summary>
    public readonly int Offset = offset;

    /// <summary>
    /// The start line of the sequence point.
    /// </summary>
    public readonly int StartLine = startLine;

    /// <summary>
    /// The start column of the sequence point.
    /// </summary>
    public readonly int StartColumn = startColumn;

    /// <summary>
    /// The end line of the sequence point.
    /// </summary>
    public readonly int EndLine = endLine;

    /// <summary>
    /// The end column of the sequence point.
    /// </summary>
    public readonly int EndColumn = endColumn;
}
