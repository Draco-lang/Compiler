using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
internal readonly partial struct SyntaxList<TNode> : IEnumerable<TNode>
    where TNode : SyntaxNode
{
    // TODO
    /// <summary>
    /// An empty <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public static readonly SyntaxList<TNode> Empty = false ? default! : throw new NotImplementedException();

    /// <summary>
    /// The number of nodes in this list.
    /// </summary>
    public int Length => this.Nodes.Length;

    /// <summary>
    /// Retrieves the node at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The 0-based index to retrieve the node from.</param>
    /// <returns>The <typeparamref name="TNode"/> at index <paramref name="index"/>.</returns>
    public TNode this[int index] => throw new NotImplementedException();

    /// <summary>
    /// The raw <see cref="SyntaxNode"/>s in this list.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> Nodes;

    public Api.Syntax.SyntaxList<TRedNode> ToRedNode<TRedNode>(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode parent)
        where TRedNode : Api.Syntax.SyntaxNode => throw new NotImplementedException();
    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<TNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
