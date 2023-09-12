using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Documentation.Extractors;

/// <summary>
/// Extracts markdown into <see cref="SymbolDocumentation"/>.
/// </summary>
internal sealed class MarkdownDocumentationExtractor
{
    /// <summary>
    /// Extracts the markdown documentation from <paramref name="containingSymbol"/>.
    /// </summary>
    /// <returns>The extracted markdown as <see cref="SymbolDocumentation"/>.</returns>
    public static SymbolDocumentation Extract(Symbol containingSymbol) =>
        new MarkdownDocumentationExtractor(containingSymbol.RawDocumentation, containingSymbol).Extract();

    private readonly string markdown;
    private readonly Symbol containingSymbol;

    private MarkdownDocumentationExtractor(string markdown, Symbol containingSymbol)
    {
        this.markdown = markdown;
        this.containingSymbol = containingSymbol;
    }

    /// <summary>
    /// Extracts the <see cref="markdown"/>.
    /// </summary>
    /// <returns>The extracted markdown as <see cref="SymbolDocumentation"/>.</returns>
    private SymbolDocumentation Extract() => new MarkdownSymbolDocumentation(this.markdown);
}
