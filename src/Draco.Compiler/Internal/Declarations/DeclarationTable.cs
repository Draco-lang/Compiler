using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// Keeps track of all declarations from the parse trees.
/// </summary>
internal sealed class DeclarationTable
{
    public static DeclarationTable Empty { get; } = new(ImmutableArray<CompilationUnitSyntax>.Empty);

    /// <summary>
    /// The merged root module.
    /// </summary>
    public MergedModuleDeclaration MergedRoot => this.mergedRoot ??= this.BuildMergedRoot();
    private MergedModuleDeclaration? mergedRoot;

    private readonly ImmutableArray<CompilationUnitSyntax> compilationUnits;

    private DeclarationTable(ImmutableArray<CompilationUnitSyntax> compilationUnits)
    {
        this.compilationUnits = compilationUnits;
    }

    // NOTE: We don't have modules specified yet, so all added syntaxes are assumed to be in a global module with empty name
    private MergedModuleDeclaration BuildMergedRoot() =>
        new(this.compilationUnits.Select(s => new SingleModuleDeclaration(string.Empty, s)).ToImmutableArray());

    /// <summary>
    /// Adds a top-level compilation unit syntax to this table.
    /// </summary>
    /// <param name="compilationUnit">The syntax to add.</param>
    /// <returns>The new table, containing <paramref name="compilationUnit"/>.</returns>
    public DeclarationTable AddCompilationUnit(CompilationUnitSyntax compilationUnit) =>
        new(this.compilationUnits.Add(compilationUnit));

    /// <summary>
    /// Retrieves the DOT graph of the declaration tree for debugging purposes.
    /// </summary>
    /// <returns>The DOT graph code for the declaration tree.</returns>
    public string ToDot()
    {
        var graph = new DotGraphBuilder<Declaration>(isDirected: true);

        void Recurse(Declaration declaration)
        {
            graph!
                .AddVertex(declaration)
                .WithLabel(declaration.Name)
                .WithXLabel(declaration.GetType().Name);
            foreach (var child in declaration.Children) graph!.AddEdge(declaration, child);
        }

        Recurse(this.MergedRoot);

        return graph.ToDot();
    }
}
