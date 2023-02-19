using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
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

    private readonly QueryDatabase db = new();

    /// <summary>
    /// The trees that are being compiled.
    /// </summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    private readonly DeclarationTable declarationTable;

    /// <summary>
    /// The name of the output assembly.
    /// </summary>
    public string? AssemblyName { get; }

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees, string? assemblyName)
    {
        this.SyntaxTrees = syntaxTrees;
        this.declarationTable = DeclarationTable.From(syntaxTrees);
        this.AssemblyName = assemblyName;
    }

    public void Dump()
    {
        Console.WriteLine("Declaration-tree:");
        Console.WriteLine(this.declarationTable.ToDot());

        Console.WriteLine("Module symbol:");
        var module = new SourceModuleSymbol(null, this.declarationTable.MergedRoot);
        foreach (var m in module.Members)
        {
            Console.WriteLine(m.Name);
        }
    }
}
