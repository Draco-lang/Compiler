using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Draco.Compiler.Internal.BoundTree;

[ExcludeFromCodeCoverage]
internal abstract partial class BoundTreeRewriter
{
    public ImmutableArray<TNode> VisitArray<TNode>(ImmutableArray<TNode> array)
        where TNode : BoundNode
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
            else
            {
                // We already have a list because of an update
                elements?.Add(node);
            }
        }
        return elements?.ToImmutable() ?? array;
    }
}
