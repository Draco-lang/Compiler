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
public readonly struct SeparatedSyntaxList<TNode> : IEnumerable<SyntaxNode>
    where TNode : SyntaxNode
{
    // TODO

    /// <summary>
    /// The number of nodes in this list.
    /// </summary>
    public int Length => throw new NotImplementedException();

    internal Internal.Syntax.SeparatedSyntaxList<TGreenNode> ToGreen<TGreenNode>()
        where TGreenNode : Internal.Syntax.SyntaxNode => throw new NotImplementedException();

    public void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();

    public IEnumerator<SyntaxNode> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
