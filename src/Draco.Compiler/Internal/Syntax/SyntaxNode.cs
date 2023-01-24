using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single node in the Draco syntax tree.
/// </summary>
internal abstract class SyntaxNode
{
    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    public abstract Api.Syntax.SyntaxNode ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent);
    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
