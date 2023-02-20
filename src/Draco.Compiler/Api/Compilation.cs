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
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
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

    /// <summary>
    /// The trees that are being compiled.
    /// </summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

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

    private readonly BinderCache binderCache;

    /// <summary>
    /// The name of the output assembly.
    /// </summary>
    public string? AssemblyName { get; }

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees, string? assemblyName)
    {
        this.SyntaxTrees = syntaxTrees;
        this.AssemblyName = assemblyName;
        this.binderCache = new(this);
    }

    public void Dump()
    {
        Console.WriteLine(this.SyntaxTrees.First().GreenRoot.ToDot());
        Console.WriteLine(this.DeclarationTable.ToDot());
        Console.WriteLine(this.GlobalModule.ToDot());

        foreach (var m in this.GlobalModule.Members)
        {
            if (m is SourceFunctionSymbol func)
            {
                var body = func.Body;
            }
        }
    }

    /// <summary>
    /// Retrieves the <see cref="Binder"/> for a given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder that corresponds to <paramref name="syntax"/>.</returns>
    internal Binder GetBinder(SyntaxNode syntax) => this.binderCache.GetBinder(syntax);

    private DeclarationTable BuildDeclarationTable() => DeclarationTable.From(this.SyntaxTrees);
    private ModuleSymbol BuildGlobalModule() => new SourceModuleSymbol(this, null, this.DeclarationTable.MergedRoot);
}
