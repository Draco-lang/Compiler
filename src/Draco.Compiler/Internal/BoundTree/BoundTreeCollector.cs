using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.BoundTree;

/// <summary>
/// Provides collection utilities over the bound tree.
/// </summary>
internal static class BoundTreeCollector
{
    public static ImmutableArray<LocalSymbol> CollectLocals(BoundNode node) =>
        LocalCollector.Collect(node);

    public static ImmutableArray<FunctionSymbol> CollectLocalFunctions(BoundNode node) =>
        LocalFunctionCollector.Collect(node);

    private sealed class LocalCollector : BoundTreeVisitor
    {
        public static ImmutableArray<LocalSymbol> Collect(BoundNode node)
        {
            var collector = new LocalCollector();
            node.Accept(collector);
            return collector.locals.ToImmutableArray();
        }

        private readonly HashSet<LocalSymbol> locals = [];

        public override void VisitBlockExpression(BoundBlockExpression node)
        {
            base.VisitBlockExpression(node);
            foreach (var local in node.Locals) this.locals.Add(local);
        }
    }

    private sealed class LocalFunctionCollector : BoundTreeVisitor
    {
        public static ImmutableArray<FunctionSymbol> Collect(BoundNode node)
        {
            var collector = new LocalFunctionCollector();
            node.Accept(collector);
            return collector.locals.ToImmutable();
        }

        private readonly ImmutableArray<FunctionSymbol>.Builder locals = ImmutableArray.CreateBuilder<FunctionSymbol>();

        public override void VisitLocalFunction(BoundLocalFunction node) =>
            this.locals.Add(node.Symbol);
    }
}
