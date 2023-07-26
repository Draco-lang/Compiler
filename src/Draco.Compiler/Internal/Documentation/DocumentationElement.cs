using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal abstract record class DocumentationElement
{
    public abstract string ToMarkdown();

    public abstract XNode ToXml();
}

internal sealed record class RawTextDocumentationElement(string RawText) : DocumentationElement
{
    public override string ToMarkdown() => this.RawText;

    public override XText ToXml() => new XText(this.RawText);
}

// TODO: Change ParameterName to have both Link and DisplayName
internal sealed record class ParameterDocumentationElement(string ParameterName, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    public override string ToMarkdown() => $"- [{this.ParameterName}]({this.ParameterName}): {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override XElement ToXml() => new XElement("param",
        new XAttribute("name", this.ParameterName),
        this.Elements.Select(x => x.ToXml()));
}

internal record class SeeDocumentationElement(string Link, string DisplayText) : DocumentationElement
{
    public SeeDocumentationElement(string Cref) : this(Cref, Cref) { }

    public override string ToMarkdown() => $"[{this.DisplayText}]({this.Link})";

    public override XElement ToXml() => new XElement("see",
        new XAttribute("cref", this.Link));
}

internal record class ParamrefDocumentationElement(string ParameterName) : SeeDocumentationElement(ParameterName)
{
    public override XElement ToXml() => new XElement("paramref",
        new XAttribute("name", this.ParameterName));
}
