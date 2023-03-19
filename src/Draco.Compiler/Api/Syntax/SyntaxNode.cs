using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public Range Range => this.range ??= this.ComputeRange();
    private Range? range;

    /// <summary>
    /// The position of the first character of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public Position StartPosition => this.Range.Start;

    /// <summary>
    /// The position after the last character of this node within the source file, excluding the trivia surrounding the node.
    /// </summary>
    public Position EndPosition => this.Range.End;

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

    // TODO: Better way?
    internal Range TranslateRelativeRange(Internal.Diagnostics.RelativeRange range)
    {
        var text = this.ToString().AsSpan();
        var start = StepPositionByText(this.Range.Start, text[..range.Offset]);
        var minWidth = Math.Min(range.Width, text.Length);
        var end = StepPositionByText(start, text.Slice(range.Offset, minWidth));
        return new(start, end);
    }

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
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(Position position)
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

    private Range ComputeRange()
    {
        var line = 0;
        var column = 0;

        Position CurrentPosition() => new(Line: line, Column: column);

        void AdvanceToken(SyntaxToken token)
        {
            if (token.Kind == TokenKind.StringNewline)
            {
                ++line;
                column = 0;
            }
            else
            {
                column += token.Text.Length;
            }
        }

        void AdvanceTrivia(SyntaxTrivia trivia)
        {
            if (trivia.Kind == TriviaKind.Newline)
            {
                ++line;
                column = 0;
            }
            else
            {
                column += trivia.Text.Length;
            }
        }

        void AdvanceTriviaList(IEnumerable<SyntaxTrivia> triviaList)
        {
            foreach (var trivia in triviaList) AdvanceTrivia(trivia);
        }

        void AssignAndAdvanceToken(SyntaxToken token)
        {
            if (token.range is null)
            {
                // Not cached yet
                AdvanceTriviaList(token.LeadingTrivia);
                var start = CurrentPosition();
                AdvanceToken(token);
                var end = CurrentPosition();
                // Cache
                token.range = new(start, end);
            }
            else
            {
                // Cached, do a shortcut
                var end = token.range.Value.End;
                line = end.Line;
                column = end.Column;
            }
            // We still need to advance trailing trivia
            AdvanceTriviaList(token.TrailingTrivia);
        }

        // Get the first token in this node
        var firstToken = this.Tokens.FirstOrDefault();
        if (firstToken is null)
        {
            // This is an empty node
            // We try to look for the predecessor in the parent
            // If there is no parent, assume starting position
            if (this.Parent is null) return default;
            // There is a parent, attempt to find the node before this one
            var beforeThis = this.Parent.Children
                .TakeWhile(n => !ReferenceEquals(n.Green, this.Green))
                .FirstOrDefault();
            if (beforeThis is null)
            {
                // There wasn't a node before this, ask for the parent
                var parentRange = this.Parent.Range;
                return new(parentRange.Start, 0);
            }
            // There was a node before this one
            return new(beforeThis.Range.End, 0);
        }

        // If there was a first token, there is a last
        var lastToken = this.Tokens.Last();

        Range MakeRange() =>
            new(Start: firstToken!.range!.Value.Start, End: lastToken!.range!.Value.End);

        // The tokens already have a range
        if (firstToken.range is not null && lastToken.range is not null) return MakeRange();

        // We need to do a pass from the start of the tree up until the end token
        foreach (var token in this.Tree.Root.Tokens)
        {
            AssignAndAdvanceToken(token);
            // We are done
            if (ReferenceEquals(token.Green, lastToken.Green)) break;
        }

        // We should have the information
        return MakeRange();
    }

    // NOTE: This might be a good general utility somewhere else?
    private static Position StepPositionByText(Position start, ReadOnlySpan<char> text)
    {
        var currLine = start.Line;
        var currCol = start.Column;
        for (var i = 0; i < text.Length; ++i)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                // Either Windows or OS-X 9 style newlines
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    // Windows-style, eat extra char
                    ++i;
                }
                // Otherwise OS-X 9 style
                ++currLine;
                currCol = 0;
            }
            else if (ch == '\n')
            {
                // Unix-style newline
                ++currLine;
                currCol = 0;
            }
            else
            {
                // NOTE: We might not want to increment in all cases
                ++currCol;
            }
        }
        return new(Line: currLine, Column: currCol);
    }
}
