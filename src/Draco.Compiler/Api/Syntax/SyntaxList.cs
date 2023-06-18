using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds.</typeparam>
public sealed class SyntaxList<TNode> : SyntaxNode, IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    private static Type GreenElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Internal.Syntax.{typeof(TNode).Name}")!;
    private static Type GreenNodeType { get; } = typeof(Internal.Syntax.SyntaxList<>).MakeGenericType(GreenElementType);
    private static ConstructorInfo GreenNodeConstructor { get; } = GreenNodeType.GetConstructor(new[]
    {
        typeof(IEnumerable<>).MakeGenericType(GreenElementType),
    })!;

    internal static IReadOnlyList<Internal.Syntax.SyntaxNode> MakeGreen(IEnumerable<Internal.Syntax.SyntaxNode> nodes) =>
        (IReadOnlyList<Internal.Syntax.SyntaxNode>)GreenNodeConstructor.Invoke(new[] { nodes })!;

    public int Count => this.GreenList.Count;
    public override IEnumerable<SyntaxNode> Children => this;
    internal override Internal.Syntax.SyntaxNode Green { get; }
    internal IReadOnlyList<Internal.Syntax.SyntaxNode> GreenList => (IReadOnlyList<Internal.Syntax.SyntaxNode>)this.Green;

    public TNode this[int index]
    {
        get
        {
            this.mappedNodes ??= new SyntaxNode?[this.GreenList.Count];
            var existing = this.mappedNodes[index];
            if (existing is null)
            {
                var prevWidth = Enumerable
                    .Range(0, index)
                    .Sum(i => this.GreenList[i].FullWidth);
                existing = this.GreenList[index].ToRedNode(this.Tree, this.Parent, this.FullPosition + prevWidth);
                this.mappedNodes[index] = existing;
            }
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
