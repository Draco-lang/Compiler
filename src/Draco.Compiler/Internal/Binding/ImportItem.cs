using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single, resolved import.
/// </summary>
internal sealed class ImportItem
{
    /// <summary>
    /// The syntax that created this import.
    /// </summary>
    public ImportDeclarationSyntax Syntax { get; }

    /// <summary>
    /// The full, resolved path paired with each path syntax element.
    /// </summary>
    public ImmutableArray<KeyValuePair<ImportPathSyntax, Symbol>> Path { get; }

    /// <summary>
    /// The imported symbols.
    /// </summary>
    public IEnumerable<Symbol> ImportedSymbols { get; }

    public ImportItem(
        ImportDeclarationSyntax syntax,
        ImmutableArray<KeyValuePair<ImportPathSyntax, Symbol>> path,
        IEnumerable<Symbol> importedSymbols)
    {
        this.Syntax = syntax;
        this.Path = path;
        this.ImportedSymbols = importedSymbols;
    }
}
