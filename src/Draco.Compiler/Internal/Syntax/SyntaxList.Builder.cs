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

        private readonly ImmutableArray<SyntaxNode>.Builder builder = ImmutableArray.CreateBuilder<SyntaxNode>();

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
        /// Clears the elements from this builder.
        /// </summary>
        public void Clear() => this.builder.Clear();

        public IEnumerator<TNode> GetEnumerator() => this.builder.Cast<TNode>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
