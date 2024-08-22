using System;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A fragment of source text with syntax higlhighting information.
/// </summary>
/// <param name="Syntax">The syntax element being colored.</param>
/// <param name="Span">The span of the colored fragment within <paramref name="Syntax"/>.</param>
/// <param name="Color">The syntax color of the fragment.</param>
/// <param name="Symbol">The symbol associated with the fragment, if any.</param>
public readonly record struct HighlightFragment(
    SyntaxNode Syntax,
    SourceSpan Span,
    SyntaxColoring Color,
    ISymbol? Symbol)
{
    public HighlightFragment(SyntaxNode syntax, SyntaxColoring color, ISymbol? symbol = null)
        : this(syntax, new SourceSpan(0, syntax.Green.Width), color, symbol)
    {
    }

    /// <summary>
    /// The absolute span of the fragment.
    /// </summary>
    public SourceSpan AbsoluteSpan => this.Span.OffsetBy(this.Syntax.Span.Start);

    /// <summary>
    /// The text of the colored fragment.
    /// </summary>
    public string Text => this.Syntax switch
    {
        SyntaxTrivia trivia => trivia.Text.Substring(this.Span.Start, this.Span.Length),
        SyntaxToken token => token.Text.Substring(this.Span.Start, this.Span.Length),
        _ => throw new NotSupportedException("not supported highlight fragment syntax"),
    };
}
