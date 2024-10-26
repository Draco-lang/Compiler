using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for <see cref="SyntaxList{TNode}"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class SyntaxList
{
    /// <summary>
    /// Creates a builder for a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <returns>The created builder.</returns>
    public static SyntaxList<TNode>.Builder CreateBuilder<TNode>()
        where TNode : SyntaxNode => [];

    /// <summary>
    /// Creates a <see cref="SyntaxList{TNode}"/> from the given elements.
    /// </summary>
    /// <typeparam name="TNode">The node element type.</typeparam>
    /// <param name="nodes">The elements to create the list from.</param>
    /// <returns>A new syntax list, containing <paramref name="nodes"/>.</returns>
    public static SyntaxList<TNode> Create<TNode>(params TNode[] nodes)
        where TNode : SyntaxNode => new([.. nodes]);
}

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
[ExcludeFromCodeCoverage]
internal sealed partial class SyntaxList<TNode>(ImmutableArray<TNode> nodes)
    : SyntaxNode, IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    public static SyntaxList<TNode> Empty { get; } = new([]);

    private static Type RedElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Api.Syntax.{typeof(TNode).Name}")!;
    private static Type RedNodeType { get; } = typeof(Api.Syntax.SyntaxList<>).MakeGenericType(RedElementType);
    private static ConstructorInfo RedNodeConstructor { get; } = RedNodeType.GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Instance,
        [
            typeof(Api.Syntax.SyntaxTree),
            typeof(Api.Syntax.SyntaxNode),
            typeof(int),
            typeof(IReadOnlyList<SyntaxNode>),
        ])!;

    /// <summary>
    /// The raw nodes of this syntax list.
    /// </summary>
    public ImmutableArray<TNode> Nodes { get; } = nodes;

    public int Count => this.Nodes.Length;
    public override IEnumerable<SyntaxNode> Children => this.Nodes;

    public TNode this[int index] => this.Nodes[index];

    public SyntaxList(IEnumerable<SyntaxNode> nodes)
        : this(nodes.Cast<TNode>().ToImmutableArray())
    {
    }

    public Builder ToBuilder() => new(this.Nodes.ToBuilder());

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxList(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxList(this);
    public override Api.Syntax.SyntaxNode ToRedNode(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent, int fullPosition) =>
        (Api.Syntax.SyntaxNode)RedNodeConstructor.Invoke([tree, parent, fullPosition, this])!;

    public IEnumerator<TNode> GetEnumerator() => this.Nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
