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
    public TriviaType Type => throw new NotImplementedException();

    public override IEnumerable<SyntaxNode> Children => throw new NotImplementedException();

    internal override Internal.Syntax.SyntaxTrivia Green { get; }

    internal SyntaxTrivia(Internal.Syntax.SyntaxTree tree, SyntaxNode? parent, Internal.Syntax.SyntaxTrivia green)
        : base(tree, parent)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
