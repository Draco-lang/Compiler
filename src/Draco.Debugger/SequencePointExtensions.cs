using System.Reflection.Metadata;

namespace Draco.Debugger;

internal static class SequencePointExtensions
{
    public static SourcePosition GetStartPosition(this SequencePoint sequencePoint) =>
        new SourcePosition(Line: sequencePoint.StartLine, Column: sequencePoint.StartColumn);

    public static SourcePosition GetEndPosition(this SequencePoint sequencePoint) =>
        new SourcePosition(Line: sequencePoint.EndLine, Column: sequencePoint.EndColumn);

    public static bool Contains(this SequencePoint sequencePoint, SourcePosition position)
    {
        var start = sequencePoint.GetStartPosition();
        var end = sequencePoint.GetEndPosition();
        return start <= position && position <= end;
    }
}
