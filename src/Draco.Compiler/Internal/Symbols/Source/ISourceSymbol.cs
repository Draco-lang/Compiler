using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a symbol that's defined in-source.
/// </summary>
internal interface ISourceSymbol
{
    /// <summary>
    /// The syntax declaring this symbol.
    /// </summary>
    public SyntaxNode? DeclarationSyntax { get; }
}
