using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A single node in the Draco syntax tree.
/// </summary>
public abstract class SyntaxNode : IEquatable<SyntaxNode>
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
    /// The <see cref="Diagnostics.Location"/> of this node, excluding the trivia surrounding the node.
    /// </summary>
    public Location Location => new Location.InFile(this.Tree.SourceText, this.Range);

    /// <summary>
    /// The <see cref="Syntax.Range"/> of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public Range Range => new(Start: this.StartPosition, End: this.EndPosition);

    /// <summary>
    /// The position of the first character of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public Position StartPosition => throw new NotImplementedException();

    /// <summary>
    /// The position after the last character of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public Position EndPosition => throw new NotImplementedException();

    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    /// <summary>
    /// All <see cref="SyntaxToken"/>s this node consists of.
    /// </summary>
    public IEnumerable<SyntaxToken> Tokens => this.Children.OfType<SyntaxToken>();

    /// <summary>
    /// The internal green node that this node wraps.
    /// </summary>
    internal abstract Internal.Syntax.SyntaxNode Green { get; }

    // TODO: Better way?
    internal Range TranslateRelativeRange(Internal.Diagnostics.RelativeRange range) =>
        throw new NotImplementedException();

    internal SyntaxNode(SyntaxTree tree, SyntaxNode? parent)
    {
        this.Tree = tree;
        this.Parent = parent;
    }

    // Equality by green nodes
    public bool Equals(SyntaxNode? other) => ReferenceEquals(this.Green, other?.Green);
    public override bool Equals(object? obj) => this.Equals(obj as SyntaxNode);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Green);

    public override string ToString() => ParseTreePrinter.ToCodeWithoutSurroundingTrivia(this.Green);

    /// <summary>
    /// Preorder traverses the subtree with this node being the root.
    /// </summary>
    /// <returns>The enumerator that performs a preorder traversal.</returns>
    public IEnumerable<SyntaxNode> PreOrderTraverse()
    {
        yield return this;
        foreach (var child in this.Children)
        {
            foreach (var e in child.PreOrderTraverse()) yield return e;
        }
    }

    /// <summary>
    /// Searches for a child node of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of child to search for.</typeparam>
    /// <param name="index">The index of the child to search for.</param>
    /// <returns>The <paramref name="index"/>th child of type <typeparamref name="TNode"/>.</returns>
    public TNode FindInChildren<TNode>(int index = 0)
        where TNode : SyntaxNode => this
        .PreOrderTraverse()
        .OfType<TNode>()
        .ElementAt(index);

    /// <summary>
    /// Enumerates this subtree, yielding all descendant nodes containing the given position.
    /// </summary>
    /// <param name="position">The position that has to be contained.</param>
    /// <returns>All subtrees containing <paramref name="position"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(Position position)
    {
        var root = this;
        while (true)
        {
            yield return root;
            foreach (var child in root.Children)
            {
                // TODO
                throw new NotImplementedException();
                //if (child.Range.Contains(position))
                //{
                //    root = child;
                //    goto found;
                //}
            }
            // No child found that contains position.
            break;
        found:;
        }
    }

    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
