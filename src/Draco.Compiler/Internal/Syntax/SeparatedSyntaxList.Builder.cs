using System;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Syntax;

internal sealed partial class SeparatedSyntaxList<TNode>
{
    /// <summary>
    /// The builder type for a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    public sealed class Builder(ImmutableArray<SyntaxNode>.Builder underlying)
    {
        private bool separatorsTurn;

        public Builder()
            : this(ImmutableArray.CreateBuilder<SyntaxNode>())
        {
        }

        /// <summary>
        /// Constructs a <see cref="SeparatedSyntaxList{TNode}"/> from the builder.
        /// </summary>
        /// <returns>The constructed <see cref="SeparatedSyntaxList{TNode}"/>.</returns>
        public SeparatedSyntaxList<TNode> ToSeparatedSyntaxList() => new(underlying.ToImmutable());

        /// <summary>
        /// Adds a value node to the builder.
        /// </summary>
        /// <param name="value">The value node to add.</param>
        public void Add(TNode value)
        {
            if (this.separatorsTurn) throw new InvalidOperationException("a separator was expected next");
            underlying.Add(value);
            this.separatorsTurn = true;
        }

        /// <summary>
        /// Adds a separator to the builder.
        /// </summary>
        /// <param name="separator">The separator to add.</param>
        public void Add(SyntaxToken separator)
        {
            if (!this.separatorsTurn) throw new InvalidOperationException("a value was expected next");
            underlying.Add(separator);
            this.separatorsTurn = false;
        }
    }
}
