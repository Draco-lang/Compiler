namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents an edit in source text.
/// </summary>
/// <param name="Source">The source text that will be edited.</param>
/// <param name="Span">The span of the text that will be replaced by <paramref name="Text"/>.</param>
/// <param name="Text">The text that should be inserted into the source text.</param>
public record class TextEdit(SourceText Source, SourceSpan Span, string Text)
{
    /// <summary>
    /// The range of the text that will be replaced by <see cref="Text"/>.
    /// </summary>
    public SyntaxRange Range => this.Source.SourceSpanToSyntaxRange(this.Span);
}
