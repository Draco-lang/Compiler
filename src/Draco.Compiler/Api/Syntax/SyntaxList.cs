using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
public sealed class SyntaxList<TNode> : IEnumerable<TNode>
    where TNode : SyntaxNode
{
    /// <summary>
    /// The number of nodes in this list.
    /// </summary>
    public int Length => this.nodes.Length;

    /// <summary>
    /// Retrieves the node at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The 0-based index to retrieve the node from.</param>
    /// <returns>The <typeparamref name="TNode"/> at index <paramref name="index"/>.</returns>
    public TNode this[int index]
    {
        get
        {
            this.mappedNodes ??= new SyntaxNode?[this.nodes.Length];
            var existing = this.mappedNodes[index];
            if (existing is null)
            {
                existing = this.nodes[index].ToRedNode(this.tree, this.parent);
                this.mappedNodes[index] = existing;
            }
            return (TNode)existing;
        }
    }

    private readonly SyntaxTree tree;
    private readonly SyntaxNode? parent;
    private readonly ImmutableArray<Internal.Syntax.SyntaxNode> nodes;
    private SyntaxNode?[]? mappedNodes = null;

    internal SyntaxList(SyntaxTree tree, SyntaxNode? parent, ImmutableArray<Internal.Syntax.SyntaxNode> nodes)
    {
        this.tree = tree;
        this.parent = parent;
        this.nodes = nodes;
    }

    internal Internal.Syntax.SyntaxList<TGreenNode> ToGreen<TGreenNode>()
        where TGreenNode : Internal.Syntax.SyntaxNode => new(this.nodes);

    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<TNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
