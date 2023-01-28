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
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
internal readonly partial struct SeparatedSyntaxList<TNode> : IEnumerable<SyntaxNode>
    where TNode : SyntaxNode
{
    /// <summary>
    /// An empty <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    public static readonly SeparatedSyntaxList<TNode> Empty = new(ImmutableArray<SyntaxNode>.Empty);

    /// <summary>
    /// The number of nodes in this list.
    /// </summary>
    public int Length => this.Nodes.Length;

    /// <summary>
    /// The width of this list in characters.
    /// </summary>
    public int Width => this.Nodes.Sum(n => n.Width);

    /// <summary>
    /// The raw <see cref="SyntaxNode"/>s in this list, including the separators.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> Nodes;

    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values
    {
        get
        {
            for (var i = 0; i < this.Nodes.Length; i += 2) yield return (TNode)this.Nodes[i];
        }
    }

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators
    {
        get
        {
            for (var i = 1; i < this.Nodes.Length; i += 2) yield return (SyntaxToken)this.Nodes[i];
        }
    }

    internal SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodes)
    {
        this.Nodes = nodes;
    }

    /// <summary>
    /// Converts this <see cref="SeparatedSyntaxList{TNode}"/> into a builder.
    /// </summary>
    /// <returns>The builder.</returns>
    public Builder ToBuilder() => new(this.Nodes);

    public Api.Syntax.SeparatedSyntaxList<TRedNode> ToRedNode<TRedNode>(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent)
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
    public SeparatedSyntaxList<TNode> Accept(SyntaxRewriter rewriter) =>
        new(this.Nodes.Select(n => n.Accept(rewriter)).ToImmutableArray());

    public IEnumerator<SyntaxNode> GetEnumerator() => this.Nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
