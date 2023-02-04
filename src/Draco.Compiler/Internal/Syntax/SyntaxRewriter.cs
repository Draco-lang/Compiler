using System.Linq;

namespace Draco.Compiler.Internal.Syntax;

internal abstract partial class SyntaxRewriter
{
    public override SyntaxNode VisitSeparatedSyntaxList<TNode>(SeparatedSyntaxList<TNode> node) =>
        new SeparatedSyntaxList<TNode>(node.Select(n => n.Accept(this)));
    public override SyntaxNode VisitSyntaxList<TNode>(SyntaxList<TNode> node) =>
        new SyntaxList<TNode>(node.Select(n => n.Accept(this)));
}
