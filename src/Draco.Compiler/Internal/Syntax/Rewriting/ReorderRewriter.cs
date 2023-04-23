using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class ReorderRewriter : SyntaxRewriter
{
    private SyntaxNode ToReorder { get; }
    private int Position { get; }

    /// <summary>
    ///  Reorders the <see cref="SyntaxList"/> where <paramref name="toReorder"/> <see cref="SyntaxNode"/> is and puts <paramref name="toReorder"/> at the <paramref name="position"/> in the original <see cref="SyntaxList"/>.
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
        for (var i = 0; i < node.Count; i++)
        {
            if (node[i] == this.ToReorder)
            {
                var list = node.ToList();
                list.RemoveAt(i);
                list.Insert(this.Position, node[i]);
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
