using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
public sealed class SyntaxTrivia : SyntaxNode
{
    /// <summary>
    /// The <see cref="TriviaType"/> of this trivia.
    /// </summary>
    public TriviaType Type => this.Green.Type;

    /// <summary>
    /// The text the trivia was produced from.
    /// </summary>
    public string Text => this.Green.Text;

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    internal override Internal.Syntax.SyntaxTrivia Green { get; }

    internal SyntaxTrivia(SyntaxTree tree, SyntaxNode? parent, Internal.Syntax.SyntaxTrivia green)
        : base(tree, parent)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxTrivia(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxTrivia(this);
}
