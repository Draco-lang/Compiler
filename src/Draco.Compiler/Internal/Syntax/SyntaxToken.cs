using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
internal sealed class SyntaxToken : SyntaxNode
{
    public override IEnumerable<SyntaxNode> Children => throw new NotImplementedException();

    public override Api.Syntax.SyntaxNode ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => throw new NotImplementedException();
    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
