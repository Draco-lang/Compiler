using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents documentation for a <see cref="Symbols.Symbol"/>.
/// </summary>
/// <param name="Sections">The <see cref="DocumentationSection"/>s this documentation contains.</param>
internal record class SymbolDocumentation(ImmutableArray<DocumentationSection> Sections)
{
    /// <summary>
    /// Empty documentation;
    /// </summary>
    public static SymbolDocumentation Empty = new SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty);

    /// <summary>
    /// The summary documentation section.
    /// </summary>
    public DocumentationSection? Summary => this.Sections.FirstOrDefault(x => x.Name.ToLower() == "summary");

    /// <summary>
    /// Creates a markdown representation of this documentation.
    /// </summary>
    /// <returns>The documentation in markdown format.</returns>
    public virtual string ToMarkdown() =>
        string.Join(Environment.NewLine, this.Sections.Select(x => x.ToMarkdown()));


    /// <summary>
    /// Creates an XML representation of this documentation.
    /// </summary>
    /// <returns>The documentation in XML format, encapsulated by a <c>documentation</c> tag.</returns>
    public virtual XElement ToXml() => new XElement("documentation",
        this.Sections.Select(x => x.ToXml()));
}

/// <summary>
/// Temporary structure for storing markdown documentation.
/// </summary>
/// <param name="Markdown">The markdown documentation.</param>
internal sealed record class MarkdownSymbolDocumentation(string Markdown) : SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty)
{
    public override string ToMarkdown() => this.Markdown;

    public override XElement ToXml() => throw new NotSupportedException();
}

// TODO: Re-add this once we have proper markdown extractor
#if false
internal sealed record class FunctionDocumentation(ImmutableArray<DocumentationSection> Sections) : SymbolDocumentation(Sections)
{
    public DocumentationSection? Return => this.Sections.FirstOrDefault(x => x.Name.ToLower() == "return");
    public ParametersDocumentationSection? Parameters => this.Sections.OfType<ParametersDocumentationSection>().FirstOrDefault();
}
#endif
