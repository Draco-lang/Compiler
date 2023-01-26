using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single node in the Draco syntax tree.
/// </summary>
internal abstract class SyntaxNode
{
    /// <summary>
    /// The width of this node in characters.
    /// </summary>
    public virtual int Width => this.Children.Select(c => c.Width).Sum();

    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    public abstract Api.Syntax.SyntaxNode ToRedNode(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent);
    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
