using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SyntaxToken : SyntaxNode
{
    /// <summary>
    /// The <see cref="TokenKind"/> of this token.
    /// </summary>
    public TokenKind Kind => this.Green.Kind;

    /// <summary>
    /// The text the token was produced from.
    /// </summary>
    public string Text => this.Green.Text;

    /// <summary>
    /// An optional associated value to this token.
    /// </summary>
    public object? Value => this.Green.Value;

    /// <summary>
    /// The <see cref="Value"/> in string representation.
    /// </summary>
    public string? ValueText => this.Green.ValueText;

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> before this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> LeadingTrivia =>
        (SyntaxList<SyntaxTrivia>)this.Green.LeadingTrivia.ToRedNode(this.Tree, this.Parent, this.FullPosition);

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> after this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> TrailingTrivia =>
        (SyntaxList<SyntaxTrivia>)this.Green.TrailingTrivia.ToRedNode(this.Tree, this.Parent, this.FullPosition + this.Green.LeadingTrivia.FullWidth + this.Green.Width);

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxToken>();

    internal override Internal.Syntax.SyntaxToken Green { get; }

    internal SyntaxToken(SyntaxTree tree, SyntaxNode? parent, int fullPosition, Internal.Syntax.SyntaxToken green)
        : base(tree, parent, fullPosition)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxToken(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxToken(this);
}
