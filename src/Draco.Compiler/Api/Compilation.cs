using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal;
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
    /// An empty compilation.
    /// </summary>
    public static Compilation Empty { get; } = Create(syntaxTrees: []);

    /// <summary>
    /// Constructs a <see cref="Compilation"/>.
    /// </summary>
    /// <param name="syntaxTrees">The <see cref="SyntaxTree"/>s to compile.</param>
    /// <param name="metadataReferences">The <see cref="MetadataReference"/>s the compiler references.</param>
    /// <param name="flags">Special compiler flags.</param>
    /// <param name="rootModulePath">The path of the root module.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="assemblyName">The output assembly name.</param>
    /// <returns>The constructed <see cref="Compilation"/>.</returns>
    public static Compilation Create(
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        CompilationFlags flags = CompilationFlags.None,
        string? rootModulePath = null,
        string? outputPath = null,
        string? assemblyName = null) => new(
        flags: flags,
        syntaxTrees: syntaxTrees,
        metadataReferences: metadataReferences,
        rootModulePath: rootModulePath,
        outputPath: outputPath,
        assemblyName: assemblyName);

    /// <summary>
    /// Constructs a <see cref="Compilation"/>.
    /// </summary>
    /// <param name="syntaxTrees">The <see cref="SyntaxTree"/>s to compile.</param>
    /// <param name="flags">Special compiler flags.</param>
    /// <param name="globalImports">The global imports for the compilation.</param>
    /// <param name="metadataReferences">The <see cref="MetadataReference"/>s the compiler references.</param>
    /// <param name="rootModulePath">The path of the root module.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="assemblyName">The output assembly name.</param>
    /// <returns>The constructed <see cref="Compilation"/>.</returns>
    internal static Compilation Create(
        ImmutableArray<SyntaxTree> syntaxTrees,
        CompilationFlags flags,
        GlobalImports globalImports,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        string? rootModulePath = null,
        string? outputPath = null,
        string? assemblyName = null,
        IReadOnlyDictionary<MetadataReference, MetadataAssemblySymbol>? metadataAssemblies = null) => new(
        syntaxTrees: syntaxTrees,
        metadataReferences: metadataReferences,
        flags: flags,
        globalImports: globalImports,
        rootModulePath: rootModulePath,
        outputPath: outputPath,
        assemblyName: assemblyName,
        metadataAssemblies: metadataAssemblies);

    /// <summary>
    /// All <see cref="Diagnostic"/> messages in the <see cref="Compilation"/>.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => this.SyntaxTrees
        .Select(this.GetSemanticModel)
        .SelectMany(model => model.Diagnostics)
        .Concat(this.GlobalDiagnosticBag)
        .ToImmutableArray();

    /// <summary>
    /// Special settings flags.
    /// </summary>
    public CompilationFlags Flags { get; }

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

    /// <summary>
    /// Global imports for the compilation.
    /// </summary>
    public GlobalImports GlobalImports { get; }

    /// <summary>
    /// The metadata assemblies this compilation references.
    /// </summary>
    internal IEnumerable<MetadataAssemblySymbol> MetadataAssemblies => this
        .MetadataReferences
        .Select(this.GetMetadataAssembly);

    // TODO: Ugly API, anything nicer?
    internal IReadOnlyDictionary<MetadataReference, MetadataAssemblySymbol> MetadataAssembliesDict => this.metadataAssemblies;

    // TODO: Currently this does NOT include the sources, which might make merging same package names
    // invalid between metadata and source. For now we don't care.
    /// <summary>
    /// The top-level merged module that contains the source along with references.
    /// </summary>
    internal ModuleSymbol RootModule =>
        LazyInitializer.EnsureInitialized(ref this.rootModule, this.BuildRootModule);
    private ModuleSymbol? rootModule;

    /// <summary>
    /// The top-level source module symbol of the compilation.
    /// </summary>
    internal ModuleSymbol SourceModule =>
        LazyInitializer.EnsureInitialized(ref this.sourceModule, this.BuildSourceModule);
    private ModuleSymbol? sourceModule;

    /// <summary>
    /// The declaration table managing the top-level declarations of the compilation.
    /// </summary>
    internal DeclarationTable DeclarationTable =>
        LazyInitializer.EnsureInitialized(ref this.declarationTable, this.BuildDeclarationTable);
    private DeclarationTable? declarationTable;

    /// <summary>
    /// A global diagnostic bag to hold non-local diagnostic messages.
    /// </summary>
    internal DiagnosticBag GlobalDiagnosticBag { get; } = new ConcurrentDiagnosticBag();
    DiagnosticBag IBinderProvider.DiagnosticBag => this.GlobalDiagnosticBag;

    /// <summary>
    /// Well-known types that need to be referenced during compilation.
    /// </summary>
    internal WellKnownTypes WellKnownTypes { get; }

    /// <summary>
    /// The type provider used for metadata references.
    /// </summary>
    internal TypeProvider TypeProvider { get; }

    private readonly BinderCache binderCache;
    private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModels = new();
    private readonly ConcurrentDictionary<MetadataReference, MetadataAssemblySymbol> metadataAssemblies = [];

    // Main ctor with all state
    private Compilation(
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<MetadataReference>? metadataReferences,
        CompilationFlags flags = CompilationFlags.None,
        GlobalImports? globalImports = null,
        string? rootModulePath = null,
        string? outputPath = null,
        string? assemblyName = null,
        ModuleSymbol? rootModule = null,
        IReadOnlyDictionary<MetadataReference, MetadataAssemblySymbol>? metadataAssemblies = null,
        ModuleSymbol? sourceModule = null,
        DeclarationTable? declarationTable = null,
        WellKnownTypes? wellKnownTypes = null,
        TypeProvider? typeProvider = null,
        BinderCache? binderCache = null)
    {
        this.Flags = flags;
        this.SyntaxTrees = syntaxTrees;
        this.MetadataReferences = metadataReferences ?? [];
        this.RootModulePath = Path.TrimEndingDirectorySeparator(rootModulePath ?? string.Empty);
        this.OutputPath = outputPath ?? ".";
        this.GlobalImports = globalImports ?? default;
        this.AssemblyName = assemblyName ?? "output";
        this.rootModule = rootModule;
        this.metadataAssemblies = new(metadataAssemblies?.AsEnumerable() ?? []);
        this.sourceModule = sourceModule;
        this.declarationTable = declarationTable;
        this.WellKnownTypes = wellKnownTypes ?? new WellKnownTypes(this);
        this.TypeProvider = typeProvider ?? new TypeProvider(this);
        this.binderCache = binderCache ?? new BinderCache(this);
    }

    /// <summary>
    /// Updates the given <paramref name="oldTree"/> with <paramref name="newTree"/>.
    /// </summary>
    /// <param name="oldTree">The old <see cref="SyntaxTree"/> to update.
    /// If null, then <paramref name="newTree"/> is considered an addition.</param>
    /// <param name="newTree">The new <see cref="SyntaxTree"/> to replace with.
    /// If null, then <paramref name="oldTree"/> will be deleted.</param>
    /// <returns>A <see cref="Compilation"/> reflecting the change.</returns>
    public Compilation UpdateSyntaxTree(SyntaxTree? oldTree, SyntaxTree? newTree)
    {
        var newSyntaxTrees = this.SyntaxTrees.ToBuilder();
        if (newTree is null)
        {
            if (oldTree is null) throw new ArgumentNullException(nameof(newTree), "either oldTree or newTree must not be null");
            newSyntaxTrees.Remove(oldTree);
        }
        else if (oldTree is null)
        {
            newSyntaxTrees.Add(newTree);
        }
        else
        {
            var treeIndex = this.SyntaxTrees.IndexOf(oldTree);
            if (treeIndex < 0) throw new ArgumentException("the specified tree was not in the compilation", nameof(oldTree));
            newSyntaxTrees[treeIndex] = newTree;
        }

        return new Compilation(
            syntaxTrees: newSyntaxTrees.ToImmutable(),
            metadataReferences: this.MetadataReferences,
            rootModulePath: this.RootModulePath,
            outputPath: this.OutputPath,
            assemblyName: this.AssemblyName,
            // Needs to be rebuilt
            rootModule: null,
            // We can carry on cached metadata assemblies, they are untouched
            metadataAssemblies: this.metadataAssemblies,
            // Needs to be rebuilt
            sourceModule: null,
            // Needs to be rebuilt
            declarationTable: null,
            // TODO: We might want to change the compilation of well-known-types?
            // Or we keep it as long as metadata refs don't change?
            // Just a cache
            wellKnownTypes: this.WellKnownTypes,
            // TODO: We might want to change the compilation of type provider?
            // Or we keep it as long as metadata refs don't change?
            // Just a cache
            typeProvider: this.TypeProvider,
            // TODO: We could definitely carry on info here, invalidating the correct things
            binderCache: null);
    }

    /// <summary>
    ///  Retrieves the <see cref="SemanticModel"/> for for a <see cref="SyntaxTree"/> within this compilation.
    /// </summary>
    /// <param name="tree">The syntax tree to get the semantic model for.</param>
    /// <returns>The semantic model for <paramref name="tree"/>.</returns>
    public SemanticModel GetSemanticModel(SyntaxTree tree) =>
        this.semanticModels.GetOrAdd(tree, tree => new(this, tree));

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
            Diagnostics: []);
    }

    internal ModuleSymbol GetModuleForSyntaxTree(SyntaxTree tree)
    {
        var filePath = SplitPath.FromFilePath(tree.SourceText.Path?.LocalPath ?? string.Empty);
        var rootPath = SplitPath.FromDirectoryPath(this.RootModulePath);

        // If we don't have root path or this tree is in memory only or the tree is outside of the root,
        // return the root module
        if (rootPath.IsEmpty
         || filePath.IsEmpty
         || !filePath.StartsWith(rootPath)) return this.SourceModule;

        // We look up the module starting from the root
        var subPath = filePath.RemovePrefix(rootPath);
        return this.SourceModule
            .Lookup(subPath.Span.ToImmutableArray())
            .OfType<ModuleSymbol>()
            .Single();
    }

    internal Binder GetBinder(SyntaxNode syntax) => this.binderCache.GetBinder(syntax);

    internal Binder GetBinder(Symbol symbol)
    {
        if (symbol.DeclaringSyntax is null)
        {
            throw new ArgumentException("symbol must have a declaration syntax", nameof(symbol));
        }

        return this.GetBinder(symbol.DeclaringSyntax);
    }

    Binder IBinderProvider.GetBinder(SyntaxNode syntax) => this.GetBinder(syntax);
    Binder IBinderProvider.GetBinder(Symbol symbol) => this.GetBinder(symbol);

    private MetadataAssemblySymbol GetMetadataAssembly(MetadataReference metadataReference) =>
        this.metadataAssemblies.GetOrAdd(metadataReference, _ => this.BuildMetadataAssembly(metadataReference));

    private DeclarationTable BuildDeclarationTable() => new(this);
    private ModuleSymbol BuildSourceModule() => new SourceModuleSymbol(this, null, this.DeclarationTable.MergedRoot);
    private ModuleSymbol BuildRootModule() => new MergedModuleSymbol(
        containingSymbol: null,
        name: string.Empty,
        modules: this
            .MetadataAssemblies
            .Cast<ModuleSymbol>()
            .Append(this.SourceModule)
            .ToImmutableArray());
    private MetadataAssemblySymbol BuildMetadataAssembly(MetadataReference metadataReference) =>
        new(this, metadataReference.MetadataReader, metadataReference.Documentation);
}
