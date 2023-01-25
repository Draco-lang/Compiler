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
    /// <summary>
    /// The raw <see cref="SyntaxNode"/>s in this list.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> Nodes;

    public Api.Syntax.SyntaxList<TRedNode> ToRedNode<TRedNode>(SyntaxTree tree, Api.Syntax.SyntaxNode parent)
        where TRedNode : Api.Syntax.SyntaxNode => throw new NotImplementedException();
    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<TNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
