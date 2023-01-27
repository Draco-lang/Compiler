using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Symbols;

/// <summary>
/// Utility for printing the result of symbol resolution.
/// </summary>
internal static class ScopeTreePrinter
{
    /// <summary>
    /// Constructs a DOT graph representation of the scope tree.
    /// </summary>
    /// <param name="db">The query database used for computations.</param>
    /// <param name="parseTree">The parse tree to print the tree for.</param>
    /// <returns>The DOT graph for the scope tree of <paramref name="parseTree"/>.</returns>
    public static string ToDot(QueryDatabase db, SyntaxTree parseTree)
    {
        var graph = new DotGraphBuilder<IScope>(isDirected: true);
        graph
            .WithName("ScopeTree")
            .WithRankDir(DotAttribs.RankDir.BottomToTop);
        graph.AllVertices().WithShape(DotAttribs.Shape.Rectangle);

        foreach (var node in parseTree.PreOrderTraverse())
        {
            // TODO: Somehow show illegal references?

            var scope = SymbolResolution.GetDefinedScopeOrNull(db, node);
            var referencedSymbol = SymbolResolution.GetReferencedSymbolOrNull(db, node);
            if (scope is null && referencedSymbol is null) continue;

            if (scope is not null)
            {
                // Scope, connect up to parent scope
                Debug.Assert(referencedSymbol is null);
                if (scope.Parent is not null) graph.GetOrAddEdge(scope, scope.Parent);
                // Also write all the symbols defined here
                graph
                    .AddVertex(scope)
                    .WithLabel($"{scope.Kind}: {string.Join(", ", scope.Declarations.Values.Select(s => s.Name))}");
            }
            else
            {
                // Reference, go from the referencing scope up to the defining scope
                Debug.Assert(referencedSymbol is not null);
                var referencingScope = SymbolResolution.GetContainingScopeOrNull(db, node);
                Debug.Assert(referencingScope is not null);
                if (referencedSymbol!.DefiningScope is not null)
                {
                    graph
                        .AddEdge(referencingScope!, referencedSymbol.DefiningScope)
                        .WithLabel(referencedSymbol.Name)
                        .WithAttribute("color", "grey")
                        .WithAttribute("style", "dashed");
                }
            }
        }

        return graph.ToDot();
    }
}
