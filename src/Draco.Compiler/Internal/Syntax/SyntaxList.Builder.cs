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
    public sealed class Builder
    {
        /// <summary>
        /// Constructs a <see cref="SyntaxList{TNode}"/> from the builder.
        /// </summary>
        /// <returns>The constructed <see cref="SyntaxList{TNode}"/>.</returns>
        public SyntaxList<TNode> ToSyntaxList() => throw new NotImplementedException();

        /// <summary>
        /// Adds a <typeparamref name="TNode"/> to this builder.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public void Add(TNode node) => throw new NotImplementedException();
    }
}
