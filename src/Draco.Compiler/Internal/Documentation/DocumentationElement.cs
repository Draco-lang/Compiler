using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal abstract record class DocumentationElement
{
    public abstract string ToMarkdown();

    public abstract string ToXml();
}

internal sealed record class RawTextDocumentationElement(string RawText) : DocumentationElement
{
    public override string ToMarkdown() => this.RawText;

    public override string ToXml() => this.RawText;
}

internal sealed record class ParameterDocumentationElement(string ParameterName, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    public override string ToMarkdown() => $"- [{this.ParameterName}]({this.ParameterName}): {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override string ToXml() => $"<param name=\"{this.ParameterName}\">{string.Join("", this.Elements.Select(x => x.ToXml()))}</param>";
}
