using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

// TODO: It seems like SyntaxTokens are not implementing any kind of Equals
// which means the Update methods are not actually reusing the existing nodes when possible.
// Prolly a bug, verify and fix.

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
internal sealed partial class SyntaxToken : SyntaxNode
{
    /// <summary>
    /// Constructs a <see cref="SyntaxToken"/> from the given data.
    /// </summary>
    /// <param name="kind">The <see cref="TokenKind"/>.</param>
    /// <param name="text">The text the token was constructed from.</param>
    /// <param name="value">The associated value of the token.</param>
    /// <returns>A new <see cref="SyntaxToken"/> with <see cref="Kind"/> <paramref name="kind"/>,
    /// <see cref="Text"/> <paramref name="text"/> and <see cref="Value"/> <paramref name="value"/>.</returns>
    public static SyntaxToken From(TokenKind kind, string? text = null, object? value = null) => new(
        kind: kind,
        text: text ?? SyntaxFacts.GetTokenText(kind) ?? throw new ArgumentNullException(nameof(text)),
        value: value,
        leadingTrivia: SyntaxList<SyntaxTrivia>.Empty,
        trailingTrivia: SyntaxList<SyntaxTrivia>.Empty);

    /// <summary>
    /// The <see cref="TokenKind"/> of this token.
    /// </summary>
    public TokenKind Kind { get; }

    /// <summary>
    /// The text the token was produced from.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// An optional associated value to this token.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// The <see cref="Value"/> in string representation.
    /// </summary>
    public string? ValueText => this.Value?.ToString();

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> before this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> LeadingTrivia { get; }

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> after this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> TrailingTrivia { get; }

    public override int FullWidth { get; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxToken>();

    public SyntaxToken(
        TokenKind kind,
        string text,
        object? value,
        SyntaxList<SyntaxTrivia> leadingTrivia,
        SyntaxList<SyntaxTrivia> trailingTrivia)
    {
        this.Kind = kind;
        this.Text = text;
        this.Value = value;
        this.LeadingTrivia = leadingTrivia;
        this.TrailingTrivia = trailingTrivia;
        this.FullWidth = leadingTrivia.FullWidth + text.Length + trailingTrivia.FullWidth;
    }

    /// <summary>
    /// Creates a builder from this token.
    /// </summary>
    /// <returns>A new builder with all data copied from this token.</returns>
    public Builder ToBuilder() => Builder.From(this);

    public override Api.Syntax.SyntaxToken ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent, int fullPosition) => new(tree, parent, fullPosition, this);
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxToken(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxToken(this);
}
