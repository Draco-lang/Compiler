using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Scripting;

/// <summary>
/// Represents the execution context of the REPL.
/// This is a mutable class, keeping track of state and evolving with the REPL session.
/// </summary>
internal sealed class ReplContext
{
    /// <summary>
    /// The global imports for the context to be used in the compilation.
    /// </summary>
    public GlobalImports GlobalImports => new(
        this.globalImports.ToImmutable(),
        this.globalAliases.ToImmutable());

    private readonly ImmutableArray<string>.Builder globalImports = ImmutableArray.CreateBuilder<string>();
    private readonly ImmutableArray<(string Name, string FullPath)>.Builder globalAliases
        = ImmutableArray.CreateBuilder<(string Name, string FullPath)>();

    /// <summary>
    /// Adds a global import to the context.
    /// </summary>
    /// <param name="path">The import path to add.</param>
    public void AddImport(string path) => this.globalImports.Add(path);

    /// <summary>
    /// Adds a symbol to be accessible in the context.
    /// Might shadows earlier symbols added.
    /// </summary>
    /// <param name="symbol">The symbol to add to the context.</param>
    public void AddSymbol(Symbol symbol)
    {
        // We remove shadowed symbols
        // NOTE: For simplicity and to not cross compilation borders, we remove by name
        this.globalAliases.RemoveAll(s => s.Name == symbol.Name);

        // Add the new symbol
        this.globalAliases.Add((symbol.Name, symbol.MetadataFullName));
    }
}
