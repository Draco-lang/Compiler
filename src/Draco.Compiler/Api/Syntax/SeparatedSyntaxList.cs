using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class SeparatedSyntaxList<TNode> : SyntaxNode, IEnumerable<SyntaxNode>
    where TNode : SyntaxNode
{
    private static Type GreenElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Internal.Syntax.{typeof(TNode).Name}")!;
    private static Type GreenNodeType { get; } = typeof(Internal.Syntax.SeparatedSyntaxList<>).MakeGenericType(GreenElementType);
    private static ConstructorInfo GreenNodeConstructor { get; } = GreenNodeType.GetConstructor(
    [
        typeof(IEnumerable<>).MakeGenericType(GreenElementType),
    ])!;

    internal static IReadOnlyList<Internal.Syntax.SyntaxNode> MakeGreen(IEnumerable<Internal.Syntax.SyntaxNode> nodes) =>
        (IReadOnlyList<Internal.Syntax.SyntaxNode>)GreenNodeConstructor.Invoke([nodes])!;

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
        var mappedNodes = LazyInitializer.EnsureInitialized(ref this.mappedNodes, () => new SyntaxNode?[this.GreenList.Count]);
        var existing = LazyInitializer.EnsureInitialized(ref mappedNodes[index], () =>
        {
            var prevWidth = this.GreenList
                .Take(index)
                .Sum(g => g.FullWidth);
            return this.GreenList[index].ToRedNode(this.Tree, this, this.FullPosition + prevWidth);
        });
        return existing;
    }
}
