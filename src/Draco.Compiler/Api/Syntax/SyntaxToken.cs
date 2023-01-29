using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
public sealed class SyntaxToken : SyntaxNode
{
    /// <summary>
    /// The <see cref="TokenType"/> of this token.
    /// </summary>
    public TokenType Type => this.Green.Type;

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
    public SyntaxList<SyntaxTrivia> LeadingTrivia => (SyntaxList<SyntaxTrivia>)this.Green.LeadingTrivia.ToRedNode(this.Tree, this.Parent);

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> after this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> TrailingTrivia => (SyntaxList<SyntaxTrivia>)this.Green.TrailingTrivia.ToRedNode(this.Tree, this.Parent);

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxToken>();

    internal override Internal.Syntax.SyntaxToken Green { get; }

    internal SyntaxToken(SyntaxTree tree, SyntaxNode? parent, Internal.Syntax.SyntaxToken green)
        : base(tree, parent)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxToken(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxToken(this);
}
