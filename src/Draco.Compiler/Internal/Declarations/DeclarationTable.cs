using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
    /// <param name="rootPath">The path to the root module.</param>
    /// <returns>The declaration table containing <paramref name="syntaxTrees"/>.</returns>
    public static DeclarationTable From(ImmutableArray<SyntaxTree> syntaxTrees, string rootPath) => new(syntaxTrees, rootPath);

    /// <summary>
    /// An empty declaration table.
    /// </summary>
    public static DeclarationTable Empty { get; } = new(ImmutableArray<SyntaxTree>.Empty, string.Empty);

    /// <summary>
    /// The merged root module.
    /// </summary>
    public MergedModuleDeclaration MergedRoot => this.mergedRoot ??= this.BuildMergedRoot();
    private MergedModuleDeclaration? mergedRoot;

    private readonly ImmutableArray<SyntaxTree> syntaxTrees;
    private readonly string rootPath;

    private DeclarationTable(ImmutableArray<SyntaxTree> syntaxTrees, string rootPath)
    {
        this.syntaxTrees = syntaxTrees;
        this.rootPath = rootPath;
    }

    private MergedModuleDeclaration BuildMergedRoot()
    {
        var modules = ImmutableArray.CreateBuilder<SingleModuleDeclaration>();
        foreach (var tree in this.syntaxTrees)
        {
            var path = tree.SourceText.Path?.OriginalString;
            var aboveRoot = Directory.GetParent(this.rootPath)?.FullName;
            if (path is null || aboveRoot is null) throw new System.NotImplementedException();
            if (!path.StartsWith(aboveRoot)) throw new System.NotImplementedException();
            var subPath = path[aboveRoot.Length..].TrimStart(Path.DirectorySeparatorChar);
            var fullName = Path.GetDirectoryName(subPath)?.TrimEnd(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '.');
            if (fullName is null) throw new System.NotImplementedException();
            modules.Add(new SingleModuleDeclaration(fullName.Split('.').Last(), fullName,(CompilationUnitSyntax)tree.Root));
        }
        var rootName = Path.GetFileName(this.rootPath.TrimEnd(Path.DirectorySeparatorChar));
        return new(rootName, rootName, modules.ToImmutable());
    }

    /// <summary>
    /// Adds a syntax-tree to this table.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to add.</param>
    /// <returns>The new table, containing declarations in <paramref name="syntaxTree"/>.</returns>
    public DeclarationTable AddCompilationUnit(SyntaxTree syntaxTree) =>
        new(this.syntaxTrees.Add(syntaxTree), this.rootPath);

    /// <summary>
    /// Adds a syntax-trees to this table.
    /// </summary>
    /// <param name="syntaxTrees">The syntax trees to add.</param>
    /// <returns>The new table, containing <paramref name="syntaxTrees"/>.</returns>
    public DeclarationTable AddCompilationUnits(IEnumerable<SyntaxTree> syntaxTrees) =>
        new(this.syntaxTrees.AddRange(syntaxTrees), this.rootPath);

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
