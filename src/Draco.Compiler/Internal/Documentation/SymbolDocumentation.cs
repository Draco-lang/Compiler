using System;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal record class SymbolDocumentation(ImmutableArray<DocumentationSection> Sections)
{
    public static SymbolDocumentation Empty = new SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty);

    public DocumentationSection? Summary => this.Sections.FirstOrDefault(x => x.Name.ToLower() == "summary");

    public string ToMarkdown() =>
        string.Join(Environment.NewLine, this.Sections.Select(x => x.ToMarkdown()));

    public string ToXml() =>
        string.Join(Environment.NewLine, this.Sections.Select(x => x.ToXml()));
}

internal sealed record class FunctionDocumentation(ImmutableArray<DocumentationSection> Sections) : SymbolDocumentation(Sections)
{
    public DocumentationSection? Return => this.Sections.FirstOrDefault(x => x.Name.ToLower() == "return");
    public ParametersDocumentationSection? Parameters => this.Sections.OfType<ParametersDocumentationSection>().FirstOrDefault();
}
