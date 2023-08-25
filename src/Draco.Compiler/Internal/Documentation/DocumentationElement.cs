using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents single documentation element.
/// </summary>
internal abstract record class DocumentationElement
{
    /// <summary>
    /// Creates a markdown representation of this documentation element.
    /// </summary>
    /// <returns>The documentation in markdown format.</returns>
    public abstract string ToMarkdown();

    /// <summary>
    /// Creates an XML representation of this documentation element.
    /// </summary>
    /// <returns>The documentation in XML format.</returns>
    public abstract XNode ToXml();
}

/// <summary>
/// Represents regular text inside documentation.
/// </summary>
/// <param name="Text">The text represented by this element.</param>
internal sealed record class TextDocumentationElement(string Text) : DocumentationElement
{
    public override string ToMarkdown() => this.Text;

    public override XText ToXml() => new XText(this.Text);
}

/// <summary>
/// A single parameter.
/// </summary>
/// <param name="ParameterLink">The link to given parameter.</param>
/// <param name="Elements">The <see cref="DocumentationElement"/>s that are contained in the description of this parameter.</param>
internal sealed record class ParameterDocumentationElement(ParameterSymbol? Parameter, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    private readonly string name = Parameter?.Name ?? string.Empty;
    private string? filePath = Parameter?.DeclaringSyntax?.Location.SourceText.Path?.LocalPath;
    private string Link => this.filePath is null
        ? string.Empty
        : $"{this.filePath}#L{this.Parameter?.DeclaringSyntax?.Location.Range?.Start.Line}";

    public override string ToMarkdown() => $"- [{this.name}]({this.Link}): {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override XElement ToXml() => new XElement("param",
        new XAttribute("name", this.name),
        this.Elements.Select(x => x.ToXml()));
}

/// <summary>
/// A single type parameter.
/// </summary>
/// <param name="ParameterLink">The link to given type parameter.</param>
/// <param name="Elements">The <see cref="DocumentationElement"/>s that are contained in the description of this type parameter.</param>
internal sealed record class TypeParameterDocumentationElement(TypeParameterSymbol? TypeParameter, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    private readonly string name = TypeParameter?.Name ?? string.Empty;
    private string? filePath = TypeParameter?.DeclaringSyntax?.Location.SourceText.Path?.LocalPath;
    private string Link => this.filePath is null
        ? string.Empty
        : $"{this.filePath}#L{this.TypeParameter?.DeclaringSyntax?.Location.Range?.Start.Line}";

    public override string ToMarkdown() => $"- [{this.name}]({this.Link}): {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override XElement ToXml() => new XElement("typeparam",
        new XAttribute("name", this.name),
        this.Elements.Select(x => x.ToXml()));
}

/// <summary>
/// A link to some symbol in code.
/// </summary>
/// <param name="ReferencedSymbol">The symbol that is linked.</param>
/// <param name="DisplayText">The text that should be displayed in the link.</param>
internal sealed record class ReferenceDocumentationElement(Symbol? ReferencedSymbol, string DisplayText) : DocumentationElement
{
    private string? filePath = ReferencedSymbol?.DeclaringSyntax?.Location.SourceText.Path?.LocalPath;
    private string Link => this.filePath is null
        ? string.Empty
        : $"{this.filePath}#L{this.ReferencedSymbol?.DeclaringSyntax?.Location.Range?.Start.Line}";

    public ReferenceDocumentationElement(Symbol? ReferencedSymbol) : this(ReferencedSymbol, ReferencedSymbol is ParameterSymbol or TypeParameterSymbol
        ? ReferencedSymbol?.Name ?? string.Empty
        : ReferencedSymbol?.FullName ?? string.Empty)
    { }

    public override string ToMarkdown() => $"[{this.DisplayText}]({this.Link})";

    public override XElement ToXml() => this.ReferencedSymbol switch
    {
        ParameterSymbol => new XElement("paramref", new XAttribute("name", this.DisplayText)),
        TypeParameterSymbol => new XElement("typeparamref", new XAttribute("name", this.DisplayText)),
        _ => new XElement("see", new XAttribute("cref", MetadataSymbol.GetPrefixedDocumentationName(this.ReferencedSymbol))),
    };
}

/// <summary>
/// Code element.
/// </summary>
/// <param name="Code">The code.</param>
/// <param name="Lang">The ID of the programming language this code is written in.</param>
internal sealed record class CodeDocumentationElement(string Code, string Lang) : DocumentationElement
{
    public override string ToMarkdown() => $"""
        ```{this.Lang}
        {this.Code}
        ```
        """;

    public override XNode ToXml() => new XElement("code", this.Code);
}
