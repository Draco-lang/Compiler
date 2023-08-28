using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Documentation.Extractors;

/// <summary>
/// Extracts markdown into <see cref="SymbolDocumentation"/>.
/// </summary>
internal sealed class MarkdownDocumentationExtractor
{
    public string Markdown { get; }
    public Symbol ContainingSymbol { get; }

    private MarkdownDocumentationExtractor(string markdown, Symbol containingSymbol)
    {
        this.Markdown = markdown;
        this.ContainingSymbol = containingSymbol;
    }

    /// <summary>
    /// Extracts the <paramref name="markdown"/>.
    /// </summary>
    /// <returns>The extracted markdown as <see cref="SymbolDocumentation"/>.</returns>
    public static SymbolDocumentation Extract(string markdown, Symbol containingSymbol) =>
        new MarkdownDocumentationExtractor(markdown, containingSymbol).Extract();

    /// <summary>
    /// Extracts the <see cref="Markdown"/>.
    /// </summary>
    /// <returns>The extracted markdown as <see cref="SymbolDocumentation"/>.</returns>
    private SymbolDocumentation Extract() => new MarkdownSymbolDocumentation(this.Markdown);
}
