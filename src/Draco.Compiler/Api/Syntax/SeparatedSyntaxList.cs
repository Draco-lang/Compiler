using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Internal;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
public sealed class SeparatedSyntaxList<TNode> : SyntaxNode, IEnumerable<SyntaxNode>
    where TNode : SyntaxNode
{
    private static Type GreenElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Internal.Syntax.{typeof(TNode).Name}")!;
    private static Type GreenNodeType { get; } = typeof(Internal.Syntax.SeparatedSyntaxList<>).MakeGenericType(GreenElementType);
    private static ConstructorInfo GreenNodeConstructor { get; } = GreenNodeType.GetConstructor(new[]
    {
        typeof(IEnumerable<>).MakeGenericType(GreenElementType),
    })!;

    internal static IReadOnlyList<Internal.Syntax.SyntaxNode> MakeGreen(IEnumerable<Internal.Syntax.SyntaxNode> nodes) =>
        (IReadOnlyList<Internal.Syntax.SyntaxNode>)GreenNodeConstructor.Invoke(new[] { nodes })!;

    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values
    {
        get
        {
            for (var i = 0; i < this.GreenList.Count; i += 2) yield return (TNode)this.GetNodeAt(i);
        }
    }

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators
    {
        get
        {
            for (var i = 1; i < this.GreenList.Count; i += 2) yield return (SyntaxToken)this.GetNodeAt(i);
        }
    }

    public override IEnumerable<SyntaxNode> Children => this;

    internal override Internal.Syntax.SyntaxNode Green { get; }
    internal IReadOnlyList<Internal.Syntax.SyntaxNode> GreenList => (IReadOnlyList<Internal.Syntax.SyntaxNode>)this.Green;

    private SyntaxNode?[]? mappedNodes = null;

    internal SeparatedSyntaxList(SyntaxTree tree, SyntaxNode? parent, int fullPosition, IReadOnlyList<Internal.Syntax.SyntaxNode> green)
        : base(tree, parent, fullPosition)
    {
        if (green is not Internal.Syntax.SyntaxNode greenNode) throw new ArgumentException("green must be a SyntaxNode", nameof(green));
        this.Green = greenNode;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSeparatedSyntaxList(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSeparatedSyntaxList(this);
    public IEnumerator<SyntaxNode> GetEnumerator() => Enumerable.Range(0, this.GreenList.Count).Select(this.GetNodeAt).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private SyntaxNode GetNodeAt(int index)
    {
        var mappedNodes = InterlockedUtils.InitializeNull(ref this.mappedNodes, () => new SyntaxNode?[this.GreenList.Count]);
        var existing = InterlockedUtils.InitializeNull(ref mappedNodes[index], () =>
        {
            var prevWidth = this.GreenList
                .Take(index)
                .Sum(g => g.FullWidth);
            return this.GreenList[index].ToRedNode(this.Tree, this.Parent, this.FullPosition + prevWidth);
        });
        return existing;
    }
}
