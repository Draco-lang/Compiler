using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements)
{
    protected string loweredName => this.Name.ToLowerInvariant();

    public virtual string ToMarkdown() => $"""
        # {this.loweredName}
        {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public virtual object ToXml() => new XElement(this.loweredName, this.Elements.Select(x => x.ToXml())); //$"""
    //    <{this.loweredName}>
    //    {string.Join("", this.Elements.Select(x => x.ToXml()))}
    //    </{this.loweredName}>
    //    """;
}

internal record class ParametersDocumentationSection(ImmutableArray<ParameterDocumentationElement> Parameters) : DocumentationSection("Parameters", Parameters.Cast<DocumentationElement>().ToImmutableArray())
{
    public override string ToMarkdown() =>
        $"""
        # parameters
        {string.Join(Environment.NewLine, this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public override object ToXml() => this.Elements.Select(x => x.ToXml());
}

internal record class TypeParametersDocumentationSection(ImmutableArray<TypeParameterDocumentationElement> TypeParameters) : DocumentationSection("TypeParameters", TypeParameters.Cast<DocumentationElement>().ToImmutableArray())
{
    public override string ToMarkdown() =>
        $"""
        # type parameters
        {string.Join(Environment.NewLine, this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public override object ToXml() => this.Elements.Select(x => x.ToXml());
}

internal record class CodeDocumentationSection(CodeDocumentationElement Code) : DocumentationSection("Code", ImmutableArray.Create<DocumentationElement>(Code))
{
    public override string ToMarkdown() => this.Code.ToMarkdown();
    public override object ToXml() => this.Code.ToXml();
}
