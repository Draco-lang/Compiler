using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class RemoveRewriter : SyntaxRewriter
{
    private readonly SyntaxNode toRemove;

    public RemoveRewriter(SyntaxNode toRemove)
    {
        this.toRemove = toRemove;
    }

    public override SyntaxList<TNode> VisitSyntaxList<TNode>(SyntaxList<TNode> node)
    {
        for (var i = 0; i < node.Count; i++)
        {
            if (node[i] == this.toRemove)
            {
                var list = node.ToList();
                list.RemoveAt(i);
                var builder = SyntaxList.CreateBuilder<TNode>();
                builder.AddRange(list);
                node = builder.ToSyntaxList();
                return node;
            }
        }
        return base.VisitSyntaxList(node);
    }

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node) => node;
}
