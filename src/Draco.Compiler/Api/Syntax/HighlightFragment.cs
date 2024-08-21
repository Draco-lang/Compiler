namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A fragment of source text with syntax higlhighting information.
/// </summary>
/// <param name="Span">The span of the fragment in the source text.</param>
/// <param name="Text">The text of the fragment.</param>
/// <param name="Color">The syntax color of the fragment.</param>
public readonly record struct HighlightFragment(SourceSpan Span, string Text, SyntaxColoring Color);
