using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for <see cref="SeparatedSyntaxList{TNode}"/>.
/// </summary>
internal static class SeparatedSyntaxList
{
    /// <summary>
    /// Creates a builder for a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <returns>The created builder.</returns>
    public static SeparatedSyntaxList<TNode>.Builder CreateBuilder<TNode>()
        where TNode : SyntaxNode => new();
}

internal readonly partial struct SeparatedSyntaxList<TNode>
{
    /// <summary>
    /// The builder type for a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    public sealed class Builder
    {
        /// <summary>
        /// Constructs a <see cref="SeparatedSyntaxList{TNode}"/> from the builder.
        /// </summary>
        /// <returns>The constructed <see cref="SeparatedSyntaxList{TNode}"/>.</returns>
        public SeparatedSyntaxList<TNode> ToSeparatedSyntaxList() => throw new NotImplementedException();
    }
}
