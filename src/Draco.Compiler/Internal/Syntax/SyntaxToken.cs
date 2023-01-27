using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
internal sealed partial class SyntaxToken : SyntaxNode
{
    /// <summary>
    /// Constructs a <see cref="SyntaxToken"/> from the given data.
    /// </summary>
    /// <param name="type">The <see cref="TokenType"/>.</param>
    /// <param name="text">The text the token was constructed from.</param>
    /// <returns>A new <see cref="SyntaxToken"/> with <see cref="Type"/> <paramref name="type"/> and
    /// <see cref="Text"/> <paramref name="text"/>.</returns>
    public static SyntaxToken From(TokenType type, string text) => new(
        type: type,
        text: text,
        value: null,
        leadingTrivia: SyntaxList<SyntaxTrivia>.Empty,
        trailingTrivia: SyntaxList<SyntaxTrivia>.Empty);

    /// <summary>
    /// The <see cref="TokenType"/> of this token.
    /// </summary>
    public TokenType Type { get; }

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

    public override int Width { get; }

    public override IEnumerable<SyntaxNode> Children => this.LeadingTrivia.Concat(this.TrailingTrivia);

    public SyntaxToken(
        TokenType type,
        string text,
        object? value,
        SyntaxList<SyntaxTrivia> leadingTrivia,
        SyntaxList<SyntaxTrivia> trailingTrivia)
    {
        this.Type = type;
        this.Text = text;
        this.Value = value;
        this.LeadingTrivia = leadingTrivia;
        this.TrailingTrivia = trailingTrivia;
        this.Width = leadingTrivia.Width + text.Length + trailingTrivia.Width;
    }

    public override Api.Syntax.SyntaxToken ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => new(tree, parent, this);
    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
