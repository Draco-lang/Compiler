using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.UntypedTree;

/// <summary>
/// The base for all untyped nodes in the untyped syntax tree.
/// </summary>
internal abstract partial class UntypedNode
{
    public SyntaxNode? Syntax { get; }

    protected UntypedNode(SyntaxNode? syntax)
    {
        this.Syntax = syntax;
    }

    public abstract void Accept(UntypedTreeVisitor visitor);
    public abstract TResult Accept<TResult>(UntypedTreeVisitor<TResult> visitor);

    protected static bool Equals<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        if (left.Length != right.Length) return false;
        return left.SequenceEqual(right);
    }
}
