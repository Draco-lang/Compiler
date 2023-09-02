using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents documentation for a <see cref="Symbols.Symbol"/>.
/// </summary>
/// <param name="Sections">The <see cref="DocumentationSection"/>s this documentation contains.</param>
internal record class SymbolDocumentation
{
    /// <summary>
    /// Empty documentation;
    /// </summary>
    public static SymbolDocumentation Empty = new SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty);

    /// <summary>
    /// The summary documentation section.
    /// </summary>
    public DocumentationSection? Summary => this.unorderedSections.FirstOrDefault(x => x.Name?.ToLower() == "summary");

    /// <summary>
    /// The sections ordered conventionally.
    /// </summary>
    public ImmutableArray<DocumentationSection> Sections => InterlockedUtils.InitializeDefault(ref this.sections, this.BuildOrderedSections);
    private ImmutableArray<DocumentationSection> sections;

    private readonly ImmutableArray<DocumentationSection> unorderedSections;

    public SymbolDocumentation(ImmutableArray<DocumentationSection> sections)
    {
        this.unorderedSections = sections;
    }

    /// <summary>
    /// Creates a markdown representation of this documentation.
    /// </summary>
    /// <returns>The documentation in markdown format.</returns>
    public virtual string ToMarkdown()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < this.Sections.Length; i++)
        {
            var section = this.Sections[i];
            builder.Append(section.Kind switch
            {
                SectionKind.Summary => string.Join(string.Empty, section.Elements.Select(x => x.ToMarkdown())),

                SectionKind.Parameters or SectionKind.TypeParameters =>
                    $"""
                    # {section.Name}
                    {string.Join(Environment.NewLine, section.Elements.Select(x => x.ToMarkdown()))}
                    """,

                SectionKind.Code => section.Elements[0].ToMarkdown(),

                _ => $"""
                     # {section.Name}
                     {string.Join(string.Empty, section.Elements.Select(x => x.ToMarkdown()))}
                     """
            });

            // Newline after each section except the last one
            if (i != this.Sections.Length - 1) builder.Append(Environment.NewLine);
        }
        return builder.ToString();
    }


    /// <summary>
    /// Creates an XML representation of this documentation.
    /// </summary>
    /// <returns>The documentation in XML format, encapsulated by a <c>documentation</c> tag.</returns>
    public virtual XElement ToXml()
    {
        var sections = new List<XNode>();
        foreach (var section in this.Sections)
        {
            switch (section.Kind)
            {
            case SectionKind.Summary:
                sections.Add(new XElement("summary", section.Elements.Select(x => x.ToXml())));
                break;
            case SectionKind.Parameters:
            case SectionKind.TypeParameters:
                sections.AddRange(section.Elements.Select(x => x.ToXml()));
                break;
            case SectionKind.Code:
                sections.Add(section.Elements[0].ToXml());
                break;
            default:
                // Note: The "Unknown" is for soft failing as string.Empty would throw
                sections.Add(new XElement(section.Name, section.Elements.Select(x => x.ToXml())));
                break;
            }
        }
        return new XElement("documentation", sections);
    }

    private ImmutableArray<DocumentationSection> BuildOrderedSections() =>
        this.unorderedSections.OrderBy(x => (int)x.Kind).ToImmutableArray();
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
