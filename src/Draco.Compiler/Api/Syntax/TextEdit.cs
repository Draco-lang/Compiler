namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents an edit in source text.
/// </summary>
/// <param name="Span">The span of the text that will be replaced by <paramref name="Text"/>.</param>
/// <param name="Text">The text that should be inserted into the source text.</param>
public record class TextEdit(SourceSpan Span, string Text);
