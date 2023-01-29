using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for <see cref="SyntaxList{TNode}"/>.
/// </summary>
internal static class SyntaxList
{
    /// <summary>
    /// Creates a builder for a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <returns>The created builder.</returns>
    public static SyntaxList<TNode>.Builder CreateBuilder<TNode>()
        where TNode : SyntaxNode => new();

    /// <summary>
    /// Creates a <see cref="SyntaxList{TNode}"/> from the given elements.
    /// </summary>
    /// <typeparam name="TNode">The node element type.</typeparam>
    /// <param name="nodes">The elements to create the list from.</param>
    /// <returns>A new syntax list, containing <paramref name="nodes"/>.</returns>
    public static SyntaxList<TNode> Create<TNode>(params TNode[] nodes)
        where TNode : SyntaxNode => new(nodes.Cast<SyntaxNode>().ToImmutableArray());
}

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
internal readonly partial struct SyntaxList<TNode> : IEnumerable<TNode>
    where TNode : SyntaxNode
{
    /// <summary>
    /// An empty <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public static readonly SyntaxList<TNode> Empty = new(ImmutableArray<SyntaxNode>.Empty);

    /// <summary>
    /// The number of nodes in this list.
    /// </summary>
    public int Length => this.Nodes.Length;

    /// <summary>
    /// The width of this list in characters.
    /// </summary>
    public int Width => this.Nodes.Sum(n => n.Width);

    /// <summary>
    /// Retrieves the node at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The 0-based index to retrieve the node from.</param>
    /// <returns>The <typeparamref name="TNode"/> at index <paramref name="index"/>.</returns>
    public TNode this[int index] => (TNode)this.Nodes[index];

    /// <summary>
    /// The raw <see cref="SyntaxNode"/>s in this list.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> Nodes;

    internal SyntaxList(ImmutableArray<SyntaxNode> nodes)
    {
        Debug.Assert(nodes.All(x => x is TNode));
        this.Nodes = nodes;
    }

    /// <summary>
    /// Converts this <see cref="SyntaxList{TNode}"/> into a builder.
    /// </summary>
    /// <returns>The builder.</returns>
    public Builder ToBuilder() => new(this.Nodes);

    public Api.Syntax.SyntaxList<TRedNode> ToRedNode<TRedNode>(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent)
        where TRedNode : Api.Syntax.SyntaxNode => new(tree, parent, this.Nodes);

    public void Accept(SyntaxVisitor visitor)
    {
        foreach (var n in this) n.Accept(visitor);
    }
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        foreach (var n in this) n.Accept(visitor);
        return default!;
    }
    public SyntaxList<TNode> Accept(SyntaxRewriter rewriter) =>
        new(this.Nodes.Select(n => n.Accept(rewriter)).ToImmutableArray());

    public IEnumerator<TNode> GetEnumerator()
    {
        for (var i = 0; i < this.Length; ++i) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
