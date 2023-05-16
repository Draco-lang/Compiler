using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
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
    /// <param name="compilation">The compilation using this declaration table.</param>
    /// <returns>The declaration table containing <paramref name="syntaxTrees"/>.</returns>
    public static DeclarationTable From(ImmutableArray<SyntaxTree> syntaxTrees, Compilation compilation) => new(syntaxTrees, compilation);

    /// <summary>
    /// An empty declaration table.
    /// </summary>
    public static DeclarationTable Empty { get; } = new(ImmutableArray<SyntaxTree>.Empty, Compilation.Create(ImmutableArray<SyntaxTree>.Empty));

    /// <summary>
    /// The merged root module.
    /// </summary>
    public MergedModuleDeclaration MergedRoot => this.mergedRoot ??= this.BuildMergedRoot();
    private MergedModuleDeclaration? mergedRoot;

    /// <summary>
    /// The root path of this declaration table.
    /// </summary>
    public string RootPath => this.compilation.RootModulePath;

    private readonly Compilation compilation;
    private readonly ImmutableArray<SyntaxTree> syntaxTrees;

    private DeclarationTable(ImmutableArray<SyntaxTree> syntaxTrees, Compilation compilation)
    {
        this.syntaxTrees = syntaxTrees;
        this.compilation = compilation;
    }

    private MergedModuleDeclaration BuildMergedRoot()
    {
        // If we don't have root path, we put all file into top level module
        if (string.IsNullOrEmpty(this.RootPath))
        {
            var singleModules = this.syntaxTrees
                .Select(s => new SingleModuleDeclaration(string.Empty, string.Empty, (CompilationUnitSyntax)s.Root))
                .ToImmutableArray();
            return new(string.Empty, string.Empty, singleModules);
        }

        var rootName = this.compilation.SplitRootModulePath.Parts.Span[^1];
        var modules = ImmutableArray.CreateBuilder<SingleModuleDeclaration>();
        foreach (var tree in this.syntaxTrees)
        {
            var path = SplitPath.FromFilePath(tree.SourceText.Path?.LocalPath ?? string.Empty);

            // In memory tree, default to root module
            if (path.IsEmpty)
            {
                modules.Add(new SingleModuleDeclaration(rootName, rootName, (CompilationUnitSyntax)tree.Root));
                continue;
            }

            // Add error if path doesn't start with root path
            if (!path.StartsWith(this.compilation.SplitRootModulePath))
            {
                this.compilation.GlobalDiagnosticBag.Add(
                Diagnostic.Create(
                    template: SymbolResolutionErrors.FilePathOutsideOfRootPath,
                    location: null,
                    path, this.RootPath));

                // Add to root so the compilation can continue
                modules.Add(new SingleModuleDeclaration(rootName, rootName, (CompilationUnitSyntax)tree.Root));
                continue;
            }

            var subPath = path.ToModuleSplitPath(this.compilation.SplitRootModulePath);
            var fullName = string.Join('.', subPath.Parts.ToArray());

            // Root module
            if (string.IsNullOrEmpty(fullName)) fullName = rootName;
            else fullName = $"{rootName}.{fullName}";
            var name = fullName.Split('.')[^1];
            modules.Add(new SingleModuleDeclaration(name, fullName, (CompilationUnitSyntax)tree.Root));
        }
        return new(rootName, rootName, modules.ToImmutable());
    }

    /// <summary>
    /// Adds a syntax-tree to this table.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to add.</param>
    /// <returns>The new table, containing declarations in <paramref name="syntaxTree"/>.</returns>
    public DeclarationTable AddCompilationUnit(SyntaxTree syntaxTree) =>
        new(this.syntaxTrees.Add(syntaxTree), this.compilation);

    /// <summary>
    /// Adds a syntax-trees to this table.
    /// </summary>
    /// <param name="syntaxTrees">The syntax trees to add.</param>
    /// <returns>The new table, containing <paramref name="syntaxTrees"/>.</returns>
    public DeclarationTable AddCompilationUnits(IEnumerable<SyntaxTree> syntaxTrees) =>
        new(this.syntaxTrees.AddRange(syntaxTrees), this.compilation);

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
