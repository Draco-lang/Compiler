using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Documentation;

internal sealed class MarkdownDocumentationExtractor
{
    public string Markdown { get; }
    public Symbol ContainingSymbol { get; }

    public MarkdownDocumentationExtractor(string markdown, Symbol containingSymbol)
    {
        this.Markdown = markdown;
        this.ContainingSymbol = containingSymbol;
    }

    public SymbolDocumentation Extract() => new MarkdownSymbolDocumentation(this.Markdown);
}
