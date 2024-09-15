using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Compiler.Internal;

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
    public ImmutableArray<Diagnostic> Diagnostics => InterlockedUtils.InitializeDefault(
        ref this.diagnostics,
        () => this.Tree.SyntaxDiagnosticTable.Get(this).ToImmutableArray());
    private ImmutableArray<Diagnostic> diagnostics;

    /// <summary>
    /// The <see cref="Diagnostics.Location"/> of this node, excluding the trivia surrounding the node.
    /// </summary>
    public Location Location => new SourceLocation(this);

    /// <summary>
    /// The position of the node, including leading trivia.
    /// </summary>
    internal int FullPosition { get; }

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

    internal SyntaxNode(SyntaxTree tree, SyntaxNode? parent, int fullPosition)
    {
        this.Tree = tree;
        this.Parent = parent;
        this.FullPosition = fullPosition;
    }

    // Equality by green nodes
    public bool Equals(SyntaxNode? other) => ReferenceEquals(this.Green, other?.Green);
    public override bool Equals(object? obj) => this.Equals(obj as SyntaxNode);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Green);

    public static bool operator ==(SyntaxNode? left, SyntaxNode? right) => Equals(left, right);
    public static bool operator !=(SyntaxNode? left, SyntaxNode? right) => !Equals(left, right);

    public override string ToString() => this.Green.ToCodeWithoutSurroundingTrivia();

    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
