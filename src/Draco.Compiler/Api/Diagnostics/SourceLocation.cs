using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents an in-source location.
/// </summary>
internal sealed class SourceLocation : Location
{
    public override SourceText SourceText { get; }
    public override SyntaxRange? Range { get; }

    public SourceLocation(SourceText sourceText, SourceSpan span)
    {
        this.SourceText = sourceText;
        this.Range = sourceText.SourceSpanToSyntaxRange(span);
    }

    public SourceLocation(SyntaxTree syntaxTree, SourceSpan span)
        : this(syntaxTree.SourceText, span)
    {
    }

    public SourceLocation(SyntaxNode node)
        : this(node.Tree, node.Span)
    {
    }

    public override string ToString()
    {
        var position = this.Range!.Value.Start;
        return $"at line {position.Line + 1}, character {position.Column + 1}";
    }
}
