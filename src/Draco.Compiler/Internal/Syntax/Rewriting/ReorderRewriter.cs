using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class ReorderRewriter : SyntaxRewriter
{
    private SyntaxNode ToReorder { get; }
    private int Position { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="toReorder">The node that will be reordered.</param>
    /// <param name="position">0-based position in the original syntax list this node will be put to.</param>
    public ReorderRewriter(SyntaxNode toReorder, int position)
    {
        this.ToReorder = toReorder;
        this.Position = position;
    }

    public override SyntaxList<TNode> VisitSyntaxList<TNode>(SyntaxList<TNode> node)
    {
        for (int i = 0; i < node.Count; i++)
        {
            if (node[i] == this.ToReorder)
            {
                var list = node.ToList();
                list.RemoveAt(i);
                list.Insert(this.Position, node[i]);
                var builder = SyntaxList.CreateBuilder<TNode>();
                builder.AddRange(list);
                node = builder.ToSyntaxList();
                break;
            }
        }
        return base.VisitSyntaxList(node);
    }

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node) => node;
}
