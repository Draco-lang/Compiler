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
    /// The number of nodes in this list.
    /// </summary>
    public int Length => throw new NotImplementedException();

    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values => throw new NotImplementedException();

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators => throw new NotImplementedException();

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
        where TGreenNode : Internal.Syntax.SyntaxNode => throw new NotImplementedException();

    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<SyntaxNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
