using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Documentation;

internal abstract record class DocumentationElement
{
    public abstract string ToMarkdown();

    public abstract XNode ToXml();
}

internal sealed record class TextDocumentationElement(string RawText) : DocumentationElement
{
    public override string ToMarkdown() => this.RawText;

    public override XText ToXml() => new XText(this.RawText);
}

internal sealed record class ParameterDocumentationElement(ParamrefDocumentationElement ParameterLink, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    public override string ToMarkdown() => $"- {this.ParameterLink.ToMarkdown()}: {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override XElement ToXml() => new XElement("param",
        new XAttribute("name", this.ParameterLink.DisplayText),
        this.Elements.Select(x => x.ToXml()));
}

internal sealed record class TypeParameterDocumentationElement(TypeParamrefDocumentationElement ParameterLink, ImmutableArray<DocumentationElement> Elements) : DocumentationElement
{
    public override string ToMarkdown() => $"- {this.ParameterLink.ToMarkdown()}: {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}";

    public override XElement ToXml() => new XElement("typeparam",
        new XAttribute("name", this.ParameterLink.DisplayText),
        this.Elements.Select(x => x.ToXml()));
}

internal record class SeeDocumentationElement(Symbol? ReferencedSymbol, string DisplayText) : DocumentationElement
{
    private string? filePath = ReferencedSymbol?.DeclaringSyntax?.Location.SourceText.Path?.LocalPath;
    private string Link => this.filePath is null
        ? string.Empty
        : $"{this.filePath}#L{this.ReferencedSymbol?.DeclaringSyntax?.Location.Range?.Start.Line}";

    public SeeDocumentationElement(Symbol? Cref) : this(Cref, Cref?.FullName ?? string.Empty) { }

    public override string ToMarkdown() => $"[{this.DisplayText}]({this.Link})";

    public override XElement ToXml() => new XElement("see",
        new XAttribute("cref", $"{this.ReferencedSymbol?.DocumentationPrefix}{this.ReferencedSymbol?.DocumentationFullName}" ?? string.Empty));
}

internal record class ParamrefDocumentationElement(Symbol? Parameter) : SeeDocumentationElement(Parameter, Parameter?.Name ?? string.Empty)
{
    public override XElement ToXml() => new XElement("paramref",
        new XAttribute("name", this.DisplayText));
}

internal record class TypeParamrefDocumentationElement(Symbol? TypeParameter) : SeeDocumentationElement(TypeParameter, TypeParameter?.Name ?? string.Empty)
{
    public override XElement ToXml() => new XElement("typeparamref",
        new XAttribute("name", this.DisplayText));
}

internal record class CodeDocumentationElement(string Code, string Lang) : DocumentationElement
{
    public override string ToMarkdown() => $"```{this.Lang}{this.Code}```";

    public override XNode ToXml() => new XElement("code", this.Code);
}
