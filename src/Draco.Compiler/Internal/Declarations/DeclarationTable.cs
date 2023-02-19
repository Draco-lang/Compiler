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
    /// <summary>
    /// Constructs a new declaration table from the given syntax trees.
    /// </summary>
    /// <param name="syntaxTrees">The syntax trees to construct the declarations from.</param>
    /// <returns>The declaration table containing <paramref name="syntaxTrees"/>.</returns>
    public static DeclarationTable From(ImmutableArray<SyntaxTree> syntaxTrees) => new(syntaxTrees);

    /// <summary>
    /// An empty declaration table.
    /// </summary>
    public static DeclarationTable Empty { get; } = new(ImmutableArray<SyntaxTree>.Empty);

    /// <summary>
    /// The merged root module.
    /// </summary>
    public MergedModuleDeclaration MergedRoot => this.mergedRoot ??= this.BuildMergedRoot();
    private MergedModuleDeclaration? mergedRoot;

    private readonly ImmutableArray<SyntaxTree> syntaxTrees;

    private DeclarationTable(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        this.syntaxTrees = syntaxTrees;
    }

    // NOTE: We don't have modules specified yet, so all added syntaxes are assumed to be in a global module with empty name
    private MergedModuleDeclaration BuildMergedRoot() =>
        new(this.syntaxTrees.Select(s => new SingleModuleDeclaration(string.Empty, (CompilationUnitSyntax)s.Root)).ToImmutableArray());

    /// <summary>
    /// Adds a syntax-tree to this table.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to add.</param>
    /// <returns>The new table, containing declarations in <paramref name="syntaxTree"/>.</returns>
    public DeclarationTable AddCompilationUnit(SyntaxTree syntaxTree) =>
        new(this.syntaxTrees.Add(syntaxTree));

    /// <summary>
    /// Adds a syntax-trees to this table.
    /// </summary>
    /// <param name="syntaxTrees">The syntax trees to add.</param>
    /// <returns>The new table, containing <paramref name="syntaxTrees"/>.</returns>
    public DeclarationTable AddCompilationUnits(IEnumerable<SyntaxTree> syntaxTrees) =>
        new(this.syntaxTrees.AddRange(syntaxTrees));

    /// <summary>
    /// Retrieves the DOT graph of the declaration tree for debugging purposes.
    /// </summary>
    /// <returns>The DOT graph code for the declaration tree.</returns>
    public string ToDot()
    {
        var graph = new DotGraphBuilder<Declaration>(isDirected: true);
        graph.WithName("DeclarationTree");

        void Recurse(Declaration declaration)
        {
            graph!
                .AddVertex(declaration)
                .WithLabel($"{declaration.GetType().Name}\n{declaration.Name}");
            foreach (var child in declaration.Children)
            {
                graph!.AddEdge(declaration, child);
                Recurse(child);
            }
        }

        Recurse(this.MergedRoot);

        return graph.ToDot();
    }
}
