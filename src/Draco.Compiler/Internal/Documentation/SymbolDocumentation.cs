using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal record class SymbolDocumentation(ImmutableArray<DocumentationSection> Sections)
{
    public static SymbolDocumentation Empty = new SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty);

    public DocumentationSection? Summary => this.Sections.FirstOrDefault(x => x.Name.ToLower() == "summary");

    public virtual string ToMarkdown() =>
        string.Join(Environment.NewLine, this.Sections.Select(x => x.ToMarkdown()));

    public virtual XElement ToXml() => new XElement("documentation",
        this.Sections.Select(x => x.ToXml()));
}

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
