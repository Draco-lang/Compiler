using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// The number of nodes in this list.
    /// </summary>
    public int Length => this.Nodes.Length;

    /// <summary>
    /// The raw <see cref="SyntaxNode"/>s in this list, including the separators.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> Nodes;

    public Api.Syntax.SeparatedSyntaxList<TRedNode> ToRedNode<TRedNode>(SyntaxTree tree, Api.Syntax.SyntaxNode parent)
        where TRedNode : Api.Syntax.SyntaxNode => throw new NotImplementedException();
    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<SyntaxNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
