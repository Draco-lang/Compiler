using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class SyntaxList<TNode> : SyntaxNode, IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    private static Type GreenElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Internal.Syntax.{typeof(TNode).Name}")!;
    private static Type GreenNodeType { get; } = typeof(Internal.Syntax.SyntaxList<>).MakeGenericType(GreenElementType);
    private static ConstructorInfo GreenNodeConstructor { get; } = GreenNodeType.GetConstructor(
    [
        typeof(IEnumerable<>).MakeGenericType(GreenElementType),
    ])!;

    internal static IReadOnlyList<Internal.Syntax.SyntaxNode> MakeGreen(IEnumerable<Internal.Syntax.SyntaxNode> nodes) =>
        (IReadOnlyList<Internal.Syntax.SyntaxNode>)GreenNodeConstructor.Invoke([nodes])!;

    public int Count => this.GreenList.Count;
    public override IEnumerable<SyntaxNode> Children => this;
    internal override Internal.Syntax.SyntaxNode Green { get; }
    internal IReadOnlyList<Internal.Syntax.SyntaxNode> GreenList => (IReadOnlyList<Internal.Syntax.SyntaxNode>)this.Green;

    public TNode this[int index]
    {
        get
        {
            var mappedNodes = LazyInitializer.EnsureInitialized(ref this.mappedNodes, () => new SyntaxNode?[this.GreenList.Count]);
            var existing = LazyInitializer.EnsureInitialized(ref mappedNodes[index], () =>
            {
                var prevWidth = this.GreenList
                    .Take(index)
                    .Sum(g => g.FullWidth);
                return this.GreenList[index].ToRedNode(this.Tree, this, this.FullPosition + prevWidth);
            });
            return (TNode)existing;
        }
    }

    private SyntaxNode?[]? mappedNodes = null;

    internal SyntaxList(SyntaxTree tree, SyntaxNode? parent, int fullPosition, IReadOnlyList<Internal.Syntax.SyntaxNode> green)
        : base(tree, parent, fullPosition)
    {
        if (green is not Internal.Syntax.SyntaxNode greenNode) throw new ArgumentException("green must be a SyntaxNode", nameof(green));
        this.Green = greenNode;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxList(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxList(this);
    public IEnumerator<TNode> GetEnumerator() => Enumerable.Range(0, this.Count).Select(i => this[i]).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
