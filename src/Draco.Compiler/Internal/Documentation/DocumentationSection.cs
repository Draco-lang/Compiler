using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements)
{
    public string ToMarkdown() =>
        string.Join("", this.Elements.Select(x => x.ToMarkdown()));

    public string ToXml() =>
        string.Join("", this.Elements.Select(x => x.ToXml()));
}

internal record class ParametersDocumentationSection(ImmutableArray<DocumentationElement> Elements) : DocumentationSection("Parameters", Elements);
