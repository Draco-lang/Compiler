using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
public sealed class SeparatedSyntaxList<TNode> : IEnumerable<SyntaxNode>
    where TNode : SyntaxNode
{
    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values
    {
        get
        {
            for (var i = 0; i < this.nodes.Length; i += 2) yield return (TNode)this.GetNodeAt(i);
        }
    }

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators
    {
        get
        {
            for (var i = 1; i < this.nodes.Length; i += 2) yield return (SyntaxToken)this.GetNodeAt(i);
        }
    }

    private readonly SyntaxTree tree;
    private readonly SyntaxNode? parent;
    private readonly ImmutableArray<Internal.Syntax.SyntaxNode> nodes;
    private SyntaxNode?[]? mappedNodes = null;

    internal SeparatedSyntaxList(SyntaxTree tree, SyntaxNode? parent, ImmutableArray<Internal.Syntax.SyntaxNode> nodes)
    {
        this.tree = tree;
        this.parent = parent;
        this.nodes = nodes;
    }

    internal Internal.Syntax.SeparatedSyntaxList<TGreenNode> ToGreen<TGreenNode>()
        where TGreenNode : Internal.Syntax.SyntaxNode => new(this.nodes);

    public void Accept(SyntaxVisitor visitor)
    {
        foreach (var n in this) n.Accept(visitor);
    }
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        foreach (var n in this) n.Accept(visitor);
        return default!;
    }

    public IEnumerator<SyntaxNode> GetEnumerator() => Enumerable.Range(0, this.nodes.Length).Select(this.GetNodeAt).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private SyntaxNode GetNodeAt(int index)
    {
        this.mappedNodes ??= new SyntaxNode?[this.nodes.Length];
        var existing = this.mappedNodes[index];
        if (existing is null)
        {
            existing = this.nodes[index].ToRedNode(this.tree, this.parent);
            this.mappedNodes[index] = existing;
        }
        return existing;
    }
}
