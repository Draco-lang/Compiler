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
using Draco.Compiler.Internal.Symbols.Source;

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
public sealed class Compilation
{
    /// <summary>
    /// Constructs a <see cref="Compilation"/>.
    /// </summary>
    /// <param name="syntaxTrees">The <see cref="SyntaxTree"/>s to compile.</param>
    /// <param name="assemblyName">The output assembly name.</param>
    /// <returns>The constructed <see cref="Compilation"/>.</returns>
    public static Compilation Create(ImmutableArray<SyntaxTree> syntaxTrees, string? assemblyName = null) => new(
        syntaxTrees: syntaxTrees,
        assemblyName: assemblyName);

    // TODO: Probably not the smartest idea, will only work for single files (likely)
    /// <summary>
    /// All <see cref="Diagnostic"/> messages in the <see cref="Compilation"/>.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => this.SyntaxTrees
        .Select(this.GetSemanticModel)
        .SelectMany(model => model.Diagnostics)
        .ToImmutableArray();

    /// <summary>
    /// The trees that are being compiled.
    /// </summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    /// <summary>
    /// The name of the output assembly.
    /// </summary>
    public string? AssemblyName { get; }

    /// <summary>
    /// The global module symbol of the compilation.
    /// </summary>
    internal ModuleSymbol GlobalModule => this.globalModule ??= this.BuildGlobalModule();
    private ModuleSymbol? globalModule;

    /// <summary>
    /// The declaration table managing the top-level declarations of the compilation.
    /// </summary>
    internal DeclarationTable DeclarationTable => this.declarationTable ??= this.BuildDeclarationTable();
    private DeclarationTable? declarationTable;

    /// <summary>
    /// A global diagnostic bag to hold non-local diagnostic messages.
    /// </summary>
    internal DiagnosticBag GlobalDiagnosticBag { get; } = new();

    private readonly BinderCache binderCache;

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees, string? assemblyName)
    {
        this.SyntaxTrees = syntaxTrees;
        this.AssemblyName = assemblyName;
        this.binderCache = new(this);
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
    /// <param name="dracoIrStream">The stream to write a textual representation of the Draco IR to.</param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(
        Stream peStream,
        Stream? pdbStream = null,
        Stream? declarationTreeStream = null,
        Stream? symbolTreeStream = null,
        Stream? dracoIrStream = null)
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
            symbolWriter.Write(this.GlobalModule.ToDot());
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
        var assembly = ModuleCodegen.Generate(this.GlobalModule);
        // Optimize the IR
        // TODO: Options for optimization
        OptimizationPipeline.Instance.Apply(assembly);

        // Write the IR, if needed
        if (dracoIrStream is not null)
        {
            var irWriter = new StreamWriter(dracoIrStream);
            irWriter.Write(assembly.ToString());
            irWriter.Flush();
        }

        // Generate CIL and PDB
        MetadataCodegen.Generate(assembly, peStream, pdbStream);

        return new(
            Success: true,
            Diagnostics: ImmutableArray<Diagnostic>.Empty);
    }

    /// <summary>
    /// Retrieves the <see cref="Binder"/> for a given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder that corresponds to <paramref name="syntax"/>.</returns>
    internal Binder GetBinder(SyntaxNode syntax) => this.binderCache.GetBinder(syntax);

    /// <summary>
    /// Retrieves the <see cref="Binder"/> for a given symbol definition.
    /// </summary>
    /// <param name="symbol">The symbol to retrieve the binder for.</param>
    /// <returns>The binder that corresponds to <paramref name="symbol"/>.</returns>
    internal Binder GetBinder(Symbol symbol)
    {
        if (symbol is SourceModuleSymbol)
        {
            return this.binderCache.ModuleBinder;
        }
        if (symbol.DeclarationSyntax is null)
        {
            throw new ArgumentException("symbol must have a declaration syntax", nameof(symbol));
        }

        return this.GetBinder(symbol.DeclarationSyntax);
    }

    private DeclarationTable BuildDeclarationTable() => DeclarationTable.From(this.SyntaxTrees);
    private ModuleSymbol BuildGlobalModule() => new SourceModuleSymbol(this, null, this.DeclarationTable.MergedRoot);
}
