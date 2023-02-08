using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.BoundTree;

/// <summary>
/// The base for all bound nodes in the bound tree.
/// </summary>
internal abstract partial class BoundNode
{
    protected static bool Equals<TNode>(ImmutableArray<TNode> left, ImmutableArray<TNode> right)
        where TNode : BoundNode
    {
        if (left.Length != right.Length) return false;
        return left.SequenceEqual(right);
    }
}
