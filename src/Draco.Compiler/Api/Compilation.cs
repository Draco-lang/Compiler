using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Source;
using ModuleSymbol = Draco.Compiler.Internal.Symbols.ModuleSymbol;

namespace Draco.Compiler.Api;

/// <summary>
/// The result type of code emission.
/// </summary>
/// <param name="Success">True, if the emission was successful without errors.</param>
/// <param name="Diagnostics">The <see cref="Diagnostic"/>s produced during emission.</param>
public readonly record struct EmitResult(
    bool Success,
    ImmutableArray<Diagnostic> Diagnostics);

// TODO: We are not exposing data-flow in any form of API yet
// That's going to be quite a bit of work, but eventually needs to be done

/// <summary>
/// Represents a single compilation session.
/// </summary>
public sealed class Compilation : IBinderProvider
{
    /// <summary>
    /// Constructs a <see cref="Compilation"/>.
    /// </summary>
    /// <param name="syntaxTrees">The <see cref="SyntaxTree"/>s to compile.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="assemblyName">The output assembly name.</param>
    /// <returns>The constructed <see cref="Compilation"/>.</returns>
    public static Compilation Create(
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        string? rootModule = null,
        string? outputPath = null,
        string? assemblyName = null) => new(
        syntaxTrees: syntaxTrees,
        metadataReferences: metadataReferences,
        rootModule: rootModule,
        outputPath: outputPath,
        assemblyName: assemblyName);

    // TODO: Should we cache semantic models? Currently we sometimes pass the entire code twice
    // just because this method instantiates a new semantic model each time, and then
    // we ask for a semantic model
    /// <summary>
    /// All <see cref="Diagnostic"/> messages in the <see cref="Compilation"/>.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => this.SyntaxTrees
        .Select(this.GetSemanticModel)
        .SelectMany(model => model.Diagnostics)
        .Concat(this.GlobalDiagnosticBag)
        .ToImmutableArray();

    /// <summary>
    /// The trees that are being compiled.
    /// </summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    /// <summary>
    /// The metadata references this compilation can reference from.
    /// </summary>
    public ImmutableArray<MetadataReference> MetadataReferences { get; }

    /// <summary>
    /// The path to the root module of this compilation.
    /// </summary>
    public string RootModulePath { get; }

    /// <summary>
    /// The output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// The name of the output assembly.
    /// </summary>
    public string AssemblyName { get; }

    // TODO: Currently this does NOT include the sources, which might make merging same package names
    // invalid between metadata and source. For now we don't care.
    /// <summary>
    /// The top-level merged module that contains the source along with references.
    /// </summary>
    internal ModuleSymbol RootModule => this.rootModule ??= this.BuildRootModule();
    private ModuleSymbol? rootModule;

    /// <summary>
    /// The metadata assemblies this compilation references.
    /// </summary>
    internal ImmutableArray<MetadataAssemblySymbol> MetadataAssemblies => this.metadataAssemblies ??= this.BuildMetadataAssemblies();
    private ImmutableArray<MetadataAssemblySymbol>? metadataAssemblies;

    /// <summary>
    /// The top-level source module symbol of the compilation.
    /// </summary>
    internal ModuleSymbol SourceModule => this.sourceModule ??= this.BuildSourceModule();
    private ModuleSymbol? sourceModule;

    /// <summary>
    /// The declaration table managing the top-level declarations of the compilation.
    /// </summary>
    internal DeclarationTable DeclarationTable => this.declarationTable ??= this.BuildDeclarationTable();
    private DeclarationTable? declarationTable;

    /// <summary>
    /// A global diagnostic bag to hold non-local diagnostic messages.
    /// </summary>
    internal DiagnosticBag GlobalDiagnosticBag { get; } = new();
    DiagnosticBag IBinderProvider.DiagnosticBag => this.GlobalDiagnosticBag;

    /// <summary>
    /// Welol-known types that need to be referenced during compilation.
    /// </summary>
    internal WellKnownTypes WellKnownTypes { get; }

    private readonly BinderCache binderCache;

    private Compilation(
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<MetadataReference>? metadataReferences,
        string? rootModule,
        string? outputPath,
        string? assemblyName)
    {
        this.SyntaxTrees = syntaxTrees;
        this.MetadataReferences = metadataReferences ?? ImmutableArray<MetadataReference>.Empty;
        this.RootModulePath = Path.TrimEndingDirectorySeparator(rootModule ?? string.Empty);
        this.OutputPath = outputPath ?? ".";
        this.AssemblyName = assemblyName ?? "output";
        this.WellKnownTypes = new(this);
        this.binderCache = new(this);
    }

    /// <summary>
    /// Replaces <see cref="SyntaxTree"/> that was created from file with <paramref name="path"/> with the <paramref name="newTree"/>.
    /// </summary>
    /// <param name="path">The path to the file the <see cref="SyntaxTree"/> was created from.</param>
    /// <param name="newTree">The <see cref="SyntaxTree"/> that will replace the original <see cref="SyntaxTree"/>.</param>
    /// <returns>New <see cref="Compilation"/> with the <see cref="SyntaxTree"/> updated.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Compilation UpdateSyntaxTree(Uri path, SyntaxTree newTree)
    {
        var oldTrees = this.SyntaxTrees.ToBuilder();
        var oldTree = oldTrees.FirstOrDefault(x => x.SourceText.Path == path);
        if (oldTree is null) throw new InvalidOperationException();
        oldTrees[oldTrees.IndexOf(oldTree)] = newTree;
        return new Compilation(oldTrees.ToImmutable(), this.MetadataReferences, this.RootModulePath, this.OutputPath, this.AssemblyName);
    }

    /// <summary>
    ///  Retrieves the <see cref="SemanticModel"/> for for a <see cref="SyntaxTree"/> within this compilation.
    /// </summary>
    /// <param name="tree">The syntax tree to get the semantic model for.</param>
    /// <returns>The semantic model for <paramref name="tree"/>.</returns>
    public SemanticModel GetSemanticModel(SyntaxTree tree) => new(this, tree);

    /// <summary>
    /// Emits compiled binary to a <see cref="Stream"/>.
    /// </summary>
    /// <param name="peStream">The stream to write the PE to.</param>
    /// <param name="pdbStream">The stream to write the PDB to.</param>
    /// <param name="declarationTreeStream">The stream to write the DOT graph of the declaration tree to.</param>
    /// <param name="symbolTreeStream">The stream to write the DOT graph of the symbol tree to.</param>
    /// <param name="irStream">The stream to write a textual representation of the IR to.</param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(
        Stream? peStream = null,
        Stream? pdbStream = null,
        Stream? declarationTreeStream = null,
        Stream? symbolTreeStream = null,
        Stream? irStream = null)
    {
        // Write the declaration tree, if needed
        if (declarationTreeStream is not null)
        {
            var declarationWriter = new StreamWriter(declarationTreeStream);
            declarationWriter.Write(this.DeclarationTable.ToDot());
            declarationWriter.Flush();
        }

        // Write the symbol tree, if needed
        if (symbolTreeStream is not null)
        {
            var symbolWriter = new StreamWriter(symbolTreeStream);
            symbolWriter.Write(this.SourceModule.ToDot());
            symbolWriter.Flush();
        }

        var existingDiags = this.Diagnostics;
        if (existingDiags.Length > 0)
        {
            return new(
                Success: false,
                Diagnostics: existingDiags);
        }

        // Generate IR
        var assembly = AssemblyCodegen.Generate(
            compilation: this,
            rootModule: this.SourceModule,
            emitSequencePoints: pdbStream is not null);
        // Optimize the IR
        // TODO: Options for optimization
        OptimizationPipeline.Instance.Apply(assembly);

        // Write the IR, if needed
        if (irStream is not null)
        {
            var irWriter = new StreamWriter(irStream);
            irWriter.Write(assembly.ToString());
            irWriter.Flush();
        }

        // Generate CIL and PDB
        if (peStream is not null) MetadataCodegen.Generate(this, assembly, peStream, pdbStream);

        return new(
            Success: true,
            Diagnostics: ImmutableArray<Diagnostic>.Empty);
    }

    internal ModuleSymbol GetCompilationUnitModule(SyntaxTree tree)
    {
        var filePath = Path.TrimEndingDirectorySeparator(tree.SourceText.Path?.LocalPath ?? string.Empty);
        var rootPath = this.DeclarationTable.RootPath;
        var rootName = this.SourceModule.FullName;

        // If we don't have root path or this tree is in memory only or the tree is outside of the root, return the root module
        if (string.IsNullOrEmpty(rootPath)
            || string.IsNullOrEmpty(filePath)
            || !filePath.StartsWith(rootPath)) return this.SourceModule;

        var subPath = filePath[rootPath.Length..].TrimStart(Path.DirectorySeparatorChar);
        var moduleName = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(subPath) ?? string.Empty).Replace(Path.DirectorySeparatorChar, '.');
        if (moduleName == string.Empty) moduleName = rootName;
        else moduleName = $"{rootName}.{moduleName}";
        return this.GetModuleSymbol(moduleName);
    }

    private ModuleSymbol GetModuleSymbol(string fullName)
    {
        ModuleSymbol Recurse(ModuleSymbol parent)
        {
            foreach (var member in parent.Members.OfType<ModuleSymbol>())
            {
                if (member.FullName == fullName)
                {
                    return member;
                }
                return Recurse(member);
            }
            throw new InvalidOperationException();
        }

        // Root module
        if (this.SourceModule.FullName == fullName) return this.SourceModule;
        return Recurse(this.SourceModule);
    }

    internal Binder GetBinder(SyntaxNode syntax) => this.binderCache.GetBinder(syntax);

    internal Binder GetBinder(Symbol symbol)
    {
        if (symbol is SourceModuleSymbol)
        {
            return this.binderCache.ModuleBinder;
        }
        if (symbol.DeclaringSyntax is null)
        {
            throw new ArgumentException("symbol must have a declaration syntax", nameof(symbol));
        }

        return this.GetBinder(symbol.DeclaringSyntax);
    }

    Binder IBinderProvider.GetBinder(SyntaxNode syntax) => this.GetBinder(syntax);
    Binder IBinderProvider.GetBinder(Symbol symbol) => this.GetBinder(symbol);

    private DeclarationTable BuildDeclarationTable() => DeclarationTable.From(this.SyntaxTrees, this.RootModulePath, this);
    private ModuleSymbol BuildSourceModule() => new SourceModuleSymbol(this, null, this.DeclarationTable.MergedRoot);
    private ImmutableArray<MetadataAssemblySymbol> BuildMetadataAssemblies() => this.MetadataReferences
        .Select(r => new MetadataAssemblySymbol(this, r.MetadataReader))
        .ToImmutableArray();
    private ModuleSymbol BuildRootModule() => new MergedModuleSymbol(
        containingSymbol: null,
        name: string.Empty,
        modules: this.MetadataAssemblies
            .Cast<ModuleSymbol>()
            .Append(this.SourceModule)
            .ToImmutableArray());
}
