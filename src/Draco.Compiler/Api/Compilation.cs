using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Query;
using CSharpCompilationOptions = Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions;

namespace Draco.Compiler.Api;

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

    private Compilation(ParseTree parseTree, string? assemblyName)
    {
        this.ParseTree = parseTree;
        this.AssemblyName = assemblyName;
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced before emission.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced before emission.</returns>
    public ImmutableArray<Diagnostic> GetDiagnostics() => this.GetSyntaxDiagnostics()
        .Concat(this.GetSemanticDiagnostics())
        .ToImmutableArray();

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced during syntax analysis.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced during syntax analysis.</returns>
    public ImmutableArray<Diagnostic> GetSyntaxDiagnostics() =>
        this.ParseTree.GetAllDiagnostics().ToImmutableArray();

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s that were produced during semantic analysis.
    /// </summary>
    /// <returns>The <see cref="Diagnostic"/>s produced during semantic analysis.</returns>
    public ImmutableArray<Diagnostic> GetSemanticDiagnostics() =>
        this.GetSemanticModel(this.ParseTree).GetAllDiagnostics().ToImmutableArray();

    /// <summary>
    /// Retrieves the <see cref="SemanticModel"/> for a tree.
    /// </summary>
    /// <param name="tree">The <see cref="ParseTree"/> root to retrieve the model for.</param>
    /// <returns>The <see cref="SemanticModel"/> with <paramref name="tree"/> as the root.</returns>
    public SemanticModel GetSemanticModel(ParseTree tree) =>
        new(this.db, tree);

    /// <summary>
    /// Emits compiled C# code to a <see cref="Stream"/>.
    /// </summary>
    /// <param name="csStream">The stream to write the C# code to.</param>
    public void EmitCSharp(Stream csStream)
    {
        var codegen = new CSharpCodegen(this.GetSemanticModel(this.ParseTree), csStream);
        codegen.Generate();
    }

    /// <summary>
    /// Emits compiled binary to a <see cref="Stream"/>.
    /// </summary>
    /// <param name="peStream">The stream to write the binary to.</param>
    /// <param name="csStream">The stream to write the compiled C# code to.</param>
    /// <param name="csCompilerOptionBuilder">Option builder for the underlying C# compiler.</param>
    public void Emit(
        Stream peStream,
        Stream? csStream = null,
        Func<CSharpCompilationOptions, CSharpCompilationOptions>? csCompilerOptionBuilder = null)
    {
        csStream ??= new MemoryStream();
        this.EmitCSharp(csStream);
        csStream.Position = 0;

        using var csStreamReader = new StreamReader(csStream);
        var csText = csStreamReader.ReadToEnd();

        var options = new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
        if (csCompilerOptionBuilder is not null) options = csCompilerOptionBuilder(options);

        var cSharpCompilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            assemblyName: this.AssemblyName ?? "output",
            syntaxTrees: new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(csText) },
            references: ReferenceAssemblies.Net60,
            options: options);
        var emitResult = cSharpCompilation.Emit(peStream);
        // TODO: Expose compilation errors
        // if (!emitResult.Success) we have errors in emitResult.Diagnostics;
    }
}
