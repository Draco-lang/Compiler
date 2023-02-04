using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for <see cref="SeparatedSyntaxList{TNode}"/>.
/// </summary>
internal static class SeparatedSyntaxList
{
    /// <summary>
    /// Creates a builder for a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <returns>The created builder.</returns>
    public static SeparatedSyntaxList<TNode>.Builder CreateBuilder<TNode>()
        where TNode : SyntaxNode => new();
}

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
internal sealed partial class SeparatedSyntaxList<TNode> : SyntaxNode, IReadOnlyList<SyntaxNode>
    where TNode : SyntaxNode
{
    private static Type RedElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Api.Syntax.{typeof(TNode).Name}")!;
    private static Type RedNodeType { get; } = typeof(Api.Syntax.SeparatedSyntaxList<>).MakeGenericType(RedElementType);
    private static ConstructorInfo RedNodeConstructor { get; } = RedNodeType.GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Instance,
        new[]
        {
            typeof(Api.Syntax.SyntaxTree),
            typeof(Api.Syntax.SyntaxNode),
            typeof(IReadOnlyList<SyntaxNode>),
        })!;

    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values
    {
        get
        {
            for (var i = 0; i < this.nodes.Length; i += 2) yield return (TNode)this.nodes[i];
        }
    }

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators
    {
        get
        {
            for (var i = 1; i < this.nodes.Length; i += 2) yield return (SyntaxToken)this.nodes[i];
        }
    }

    int IReadOnlyCollection<SyntaxNode>.Count => this.nodes.Length;
    SyntaxNode IReadOnlyList<SyntaxNode>.this[int index] => this.nodes[index];

    public override IEnumerable<SyntaxNode> Children => this.nodes;

    private readonly ImmutableArray<SyntaxNode> nodes;

    public SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodes)
    {
        this.nodes = nodes;
    }

    public SeparatedSyntaxList(IEnumerable<SyntaxNode> nodes)
        : this(nodes.ToImmutableArray())
    {
    }

    public Builder ToBuilder() => new(this.nodes.ToBuilder());

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSeparatedSyntaxList(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSeparatedSyntaxList(this);
    public override Api.Syntax.SyntaxNode ToRedNode(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent) =>
        (Api.Syntax.SyntaxNode)RedNodeConstructor.Invoke(new object?[] { tree, parent, this })!;

    public IEnumerator<SyntaxNode> GetEnumerator() => this.nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
