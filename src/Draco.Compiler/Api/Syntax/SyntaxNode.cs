using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single node in the Draco syntax tree.
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>
    /// The <see cref="SyntaxTree"/> this node belongs to.
    /// </summary>
    public SyntaxTree Tree { get; }

    /// <summary>
    /// The parent <see cref="SyntaxNode"/> of this one.
    /// </summary>
    public SyntaxNode? Parent { get; }

    /// <summary>
    /// The <see cref="Diagnostics.Location"/> of this node.
    /// </summary>
    public Location Location => throw new NotImplementedException();

    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    /// <summary>
    /// All <see cref="SyntaxToken"/>s this node consists of.
    /// </summary>
    public IEnumerable<SyntaxToken> Tokens => throw new NotImplementedException();

    /// <summary>
    /// The internal green node that this node wraps.
    /// </summary>
    internal abstract Internal.Syntax.SyntaxNode Green { get; }

    internal SyntaxNode(SyntaxTree tree, SyntaxNode? parent)
    {
        this.Tree = tree;
        this.Parent = parent;
    }

    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
