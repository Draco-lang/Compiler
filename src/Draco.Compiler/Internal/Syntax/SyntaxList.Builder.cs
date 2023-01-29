using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

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
