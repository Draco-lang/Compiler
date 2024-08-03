using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single, resolved import.
/// </summary>
internal sealed class ImportItem(
    ImportDeclarationSyntax syntax,
    ImmutableArray<KeyValuePair<ImportPathSyntax, Symbol>> path,
    IEnumerable<Symbol> importedSymbols)
{
    /// <summary>
    /// The syntax that created this import.
    /// </summary>
    public ImportDeclarationSyntax Syntax { get; } = syntax;

    /// <summary>
    /// The full, resolved path paired with each path syntax element.
    /// </summary>
    public ImmutableArray<KeyValuePair<ImportPathSyntax, Symbol>> Path { get; } = path;

    /// <summary>
    /// The imported symbols.
    /// </summary>
    public IEnumerable<Symbol> ImportedSymbols { get; } = importedSymbols;
}
