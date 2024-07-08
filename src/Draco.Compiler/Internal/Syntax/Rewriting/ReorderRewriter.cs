using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

/// <summary>
/// Reorders the <see cref="SyntaxList"/> where <paramref name="toReorder"/> <see cref="SyntaxNode"/> is and puts <paramref name="toReorder"/> at the <paramref name="position"/> in the original <see cref="SyntaxList"/>.
/// </summary>
/// <param name="toReorder">The node that will be reordered.</param>
/// <param name="position">0-based position in the original syntax list this node will be put to.</param>
internal sealed class ReorderRewriter(SyntaxNode toReorder, int position) : SyntaxRewriter
{
    public override SyntaxList<TNode> VisitSyntaxList<TNode>(SyntaxList<TNode> node)
    {
        for (var i = 0; i < node.Count; i++)
        {
            if (node[i] == toReorder)
            {
                var list = node.ToList();
                list.RemoveAt(i);
                list.Insert(position, node[i]);
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
