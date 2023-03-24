using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;

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
    /// The diagnostics on this tree node.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics =>
        this.diagnostics ??= this.Tree.SyntaxDiagnosticTable.Get(this).ToImmutableArray();
    private ImmutableArray<Diagnostic>? diagnostics;

    /// <summary>
    /// The <see cref="Diagnostics.Location"/> of this node, excluding the trivia surrounding the node.
    /// </summary>
    public Location Location => new SourceLocation(this);

    /// <summary>
    /// The position of the node, including leading trivia.
    /// </summary>
    internal int FullPosition
    {
        get
        {
            if (this.fullPosition is null) this.Tree.ComputeFullPositions();
            return this.fullPosition!.Value;
        }
    }
    private int? fullPosition;

    /// <summary>
    /// The position of the node, excluding leading trivia.
    /// </summary>
    internal int Position
    {
        get
        {
            var position = this.FullPosition;
            var leadingTrivia = this.Green.FirstToken?.LeadingTrivia;
            if (leadingTrivia is not null) position += leadingTrivia.FullWidth;
            return position;
        }
    }

    internal void SetFullPosition(int fullPosition) => this.fullPosition = fullPosition;

    /// <summary>
    /// The span of this syntax node, excluding the trivia surrounding the node.
    /// </summary>
    public SourceSpan Span => new(Start: this.Position, Length: this.Green.Width);

    /// <summary>
    /// The <see cref="SyntaxRange"/> of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public SyntaxRange Range => this.Tree.SourceText.SourceSpanToSyntaxRange(this.Span);

    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    /// <summary>
    /// All <see cref="SyntaxToken"/>s this node consists of.
    /// </summary>
    public IEnumerable<SyntaxToken> Tokens => this.PreOrderTraverse().OfType<SyntaxToken>();

    /// <summary>
    /// The documentation attacked before this node.
    /// </summary>
    public string Documentation => this.Green.Documentation;

    /// <summary>
    /// The internal green node that this node wraps.
    /// </summary>
    internal abstract Internal.Syntax.SyntaxNode Green { get; }

    internal SyntaxNode(SyntaxTree tree, SyntaxNode? parent)
    {
        this.Tree = tree;
        this.Parent = parent;
    }

    // Equality by green nodes
    public bool Equals(SyntaxNode? other) => ReferenceEquals(this.Green, other?.Green);
    public override bool Equals(object? obj) => this.Equals(obj as SyntaxNode);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Green);

    public override string ToString() => this.Green.ToCodeWithoutSurroundingTrivia();

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
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(SyntaxPosition position)
    {
        var root = this;
        while (true)
        {
            yield return root;
            foreach (var child in root.Children)
            {
                if (child.Range.Contains(position))
                {
                    root = child;
                    goto found;
                }
            }
            // No child found that contains position.
            break;
        found:;
        }
    }

    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
