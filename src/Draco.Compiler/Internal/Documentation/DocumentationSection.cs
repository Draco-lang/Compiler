using System;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Documentation;

internal record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements)
{
    protected string loweredName => this.Name.ToLowerInvariant();

    public virtual string ToMarkdown() => $"""
        # {this.loweredName}
        {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public virtual string ToXml() => $"""
        <{this.loweredName}>
        {string.Join("", this.Elements.Select(x => x.ToXml()))}
        </{this.loweredName}>
        """;
}

internal record class ParametersDocumentationSection(ImmutableArray<ParameterDocumentationElement> Parameters) : DocumentationSection("Parameters", Parameters.Cast<DocumentationElement>().ToImmutableArray())
{
    public override string ToMarkdown() =>
        $"""
        # parameters
        {string.Join(Environment.NewLine, this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public override string ToXml() =>
        string.Join(Environment.NewLine, this.Elements.Select(x => x.ToXml()));
}
