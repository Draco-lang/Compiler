using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents a location relative to some syntax element.
/// </summary>
internal sealed class RelativeLocation(SyntaxNode relativeTo, Location originalLocation) : Location
{
    public override SourceText SourceText => originalLocation.SourceText;
    public override SyntaxRange? Range => originalLocation.Range?.RelativeTo(relativeTo.Range.Start);

    public override string ToString()
    {
        var range = this.Range;
        if (range is null) return originalLocation.ToString()!;

        var position = range.Value.Start;
        return $"at line {position.Line + 1}, character {position.Column + 1}";
    }
}
