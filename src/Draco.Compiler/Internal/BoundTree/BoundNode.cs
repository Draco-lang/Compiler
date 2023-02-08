using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.BoundTree;

/// <summary>
/// The base for all bound nodes in the bound tree.
/// </summary>
internal abstract partial class BoundNode
{
    public SyntaxNode? Syntax { get; }

    protected BoundNode(SyntaxNode? syntax)
    {
        this.Syntax = syntax;
    }

    protected static bool Equals<TNode>(ImmutableArray<TNode> left, ImmutableArray<TNode> right)
        where TNode : BoundNode
    {
        if (left.Length != right.Length) return false;
        return left.SequenceEqual(right);
    }
}
