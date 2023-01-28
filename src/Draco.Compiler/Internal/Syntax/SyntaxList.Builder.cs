using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

internal readonly partial struct SyntaxList<TNode>
{
    /// <summary>
    /// The builder type for a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public sealed class Builder : IEnumerable<TNode>
    {
        /// <summary>
        /// The number of nodes added to the builder.
        /// </summary>
        public int Count => this.builder.Count;

        private readonly ImmutableArray<SyntaxNode>.Builder builder;

        public Builder()
        {
            this.builder = ImmutableArray.CreateBuilder<SyntaxNode>();
        }

        public Builder(ImmutableArray<SyntaxNode> initial)
        {
            this.builder = initial.ToBuilder();
        }

        /// <summary>
        /// Constructs a <see cref="SyntaxList{TNode}"/> from the builder.
        /// </summary>
        /// <returns>The constructed <see cref="SyntaxList{TNode}"/>.</returns>
        public SyntaxList<TNode> ToSyntaxList() => this.Count == 0 ? Empty : new(this.builder.ToImmutable());

        /// <summary>
        /// Adds a <typeparamref name="TNode"/> to this builder.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public void Add(TNode node) => this.builder.Add(node);

        /// <summary>
        /// Adds a sequence of <typeparamref name="TNode"/>s to this builder.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        public void AddRange(IEnumerable<TNode> nodes) => this.builder.AddRange(nodes);

        /// <summary>
        /// Adds a sequence of <typeparamref name="TNode"/>s to this builder.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        public void AddRange(SyntaxList<TNode> nodes) => this.builder.AddRange(nodes.Nodes);

        /// <summary>
        /// Clears the elements from this builder.
        /// </summary>
        public void Clear() => this.builder.Clear();

        public IEnumerator<TNode> GetEnumerator() => this.builder.Cast<TNode>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
