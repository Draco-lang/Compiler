using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Syntax;

internal abstract partial class SyntaxRewriter
{
    public override SyntaxList<TNode> VisitSyntaxList<TNode>(SyntaxList<TNode> node)
    {
        var rewritten = this.RewriteArray(node.Nodes);
        if (rewritten is null) return node;
        return new(rewritten.Value);
    }

    public override SeparatedSyntaxList<TNode> VisitSeparatedSyntaxList<TNode>(SeparatedSyntaxList<TNode> node)
    {
        var rewritten = this.RewriteArray(node.Nodes);
        if (rewritten is null) return node;
        return new(rewritten.Value);
    }

    private ImmutableArray<TNode>? RewriteArray<TNode>(ImmutableArray<TNode> array)
        where TNode : SyntaxNode
    {
        // Lazy construction, only create the builder when absolutely needed
        var elements = null as ImmutableArray<TNode>.Builder;
        foreach (var node in array)
        {
            var rewritten = node.Accept(this);
            if (!Equals(node, rewritten))
            {
                // There was an update
                if (elements is null)
                {
                    elements = ImmutableArray.CreateBuilder<TNode>();
                    // Add all previous
                    elements.AddRange(array.TakeWhile(n => !Equals(n, node)));
                }
                elements.Add((TNode)rewritten);
            }
            else if (elements is not null)
            {
                // We already have a list because of an update
                elements.Add(node);
            }
        }
        return elements?.ToImmutable();
    }
}
