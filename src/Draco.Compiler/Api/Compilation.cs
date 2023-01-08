using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.FlowAnalysis;
using Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Semantics.Symbols;

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
    /// <param name="parseTree">The <see cref="Syntax.ParseTree"/> to compile.</param>
    /// <param name="assemblyName">The output assembly name.</param>
    /// <returns>The constructed <see cref="Compilation"/>.</returns>
    public static Compilation Create(ParseTree parseTree, string? assemblyName = null) => new(
        parseTree: parseTree,
        assemblyName: assemblyName);

    private readonly QueryDatabase db = new();

    /// <summary>
    /// The tree that is being compiled.
    /// </summary>
    public ParseTree ParseTree { get; }

    /// <summary>
    /// The name of the output assembly.
    /// </summary>
    public string? AssemblyName { get; }

    /// <summary>
    /// All <see cref="Diagnostic"/> messages in the <see cref="Compilation"/>.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => this.GetDiagnostics();

    private Compilation(ParseTree parseTree, string? assemblyName)
    {
        this.ParseTree = parseTree;
        this.AssemblyName = assemblyName;
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced before emission.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced before emission.</returns>
    internal ImmutableArray<Diagnostic> GetDiagnostics() => this.GetSyntaxDiagnostics()
        .Concat(this.GetSemanticDiagnostics())
        .ToImmutableArray();

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced during syntax analysis.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced during syntax analysis.</returns>
    public ImmutableArray<Diagnostic> GetSyntaxDiagnostics() =>
        this.ParseTree.Diagnostics.ToImmutableArray();

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced during semantic analysis.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced during semantic analysis.</returns>
    public ImmutableArray<Diagnostic> GetSemanticDiagnostics() =>
        this.GetSemanticModel().Diagnostics.ToImmutableArray();

    /// <summary>
    /// Retrieves the <see cref="SemanticModel"/> for this compilation.
    /// </summary>
    /// <returns>The <see cref="SemanticModel"/> for this compilation.</returns>
    public SemanticModel GetSemanticModel() =>
        new(this.db, this.ParseTree);

    /// <summary>
    /// Emits compiled binary to a <see cref="Stream"/>.
    /// </summary>
    /// <param name="peStream">The stream to write the PE to.</param>
    /// <param name="dracoIrStream">The stream to write a textual representation of the Draco IR to.</param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(Stream peStream, Stream? dracoIrStream = null)
    {
        var existingDiags = this.GetDiagnostics();
        if (existingDiags.Length > 0)
        {
            return new(
                Success: false,
                Diagnostics: existingDiags);
        }

        // Get AST
        var ast = ParseTreeToAst.ToAst(this.db, this.ParseTree.Root);
        // Lower it
        ast = AstLowering.Lower(this.db, ast);
        // Generate Draco IR
        var asm = new Assembly(this.AssemblyName ?? "output");
        DracoIrCodegen.Generate(asm, ast);
        // Optimize the IR
        // TODO: Options for optimization
        OptimizationPipeline.Instance.Apply(asm);
        // Write the IR, if needed
        if (dracoIrStream is not null)
        {
            var irWriter = new StreamWriter(dracoIrStream);
            irWriter.Write(asm.ToString());
            irWriter.Flush();
        }
        // Generate CIL
        CilCodegen.Generate(asm, peStream);

        return new(
            Success: true,
            Diagnostics: ImmutableArray<Diagnostic>.Empty);
    }
}
