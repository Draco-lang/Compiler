using System.Collections.Generic;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
public sealed class SyntaxTrivia : SyntaxNode
{
    /// <summary>
    /// The <see cref="TriviaKind"/> of this trivia.
    /// </summary>
    public TriviaKind Kind => this.Green.Kind;

    /// <summary>
    /// The text the trivia was produced from.
    /// </summary>
    public string Text => this.Green.Text;

    public override IEnumerable<SyntaxNode> Children => [];

    internal override Internal.Syntax.SyntaxTrivia Green { get; }

    internal SyntaxTrivia(SyntaxTree tree, SyntaxNode? parent, int fullPosition, Internal.Syntax.SyntaxTrivia green)
        : base(tree, parent, fullPosition)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxTrivia(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxTrivia(this);
}
