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
    public override IEnumerable<SyntaxNode> Children => throw new NotImplementedException();

    internal override Internal.Syntax.SyntaxToken Green { get; }

    internal SyntaxToken(Internal.Syntax.SyntaxTree tree, SyntaxNode? parent, Internal.Syntax.SyntaxToken green)
        : base(tree, parent)
    {
        this.Green = green;
    }

    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
