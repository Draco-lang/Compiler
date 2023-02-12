using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.BoundTree;

internal abstract partial class BoundTreeRewriter
{
    // TODO: This needs to be fixed, arrays can not be compared with Equals
    // BoundNode.Equals is already ready for this, maybe use that?
    public ImmutableArray<TNode> VisitArray<TNode>(ImmutableArray<TNode> array)
        where TNode : BoundNode
    {
        var elements = null as ImmutableArray<TNode>.Builder;
        foreach (var node in array)
        {
            var rewritten = node.Accept(this);
            if (!Equals(node, rewritten))
            {
                // There was an update
                elements ??= ImmutableArray.CreateBuilder<TNode>();
                elements.Add((TNode)rewritten);
            }
        }
        return elements is null
            ? array
            : elements.ToImmutable();
    }
}
