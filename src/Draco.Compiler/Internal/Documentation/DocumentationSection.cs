using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
/// <param name="Name">The name of the section</param>
/// <param name="Elements">The <see cref="DocumentationElement"/>s this section contains.</param>
internal record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements)
{
    protected string loweredName => this.Name.ToLowerInvariant();

    /// <summary>
    /// Creates a markdown representation of this documentation section.
    /// </summary>
    /// <returns>The documentation in markdown format.</returns>
    public virtual string ToMarkdown() => $"""
        # {this.loweredName}
        {string.Join("", this.Elements.Select(x => x.ToMarkdown()))}
        """;

    /// <summary>
    /// Creates an XML representation of this documentation section.
    /// </summary>
    /// <returns>The documentation in XML format.</returns>
    public virtual object ToXml() => new XElement(this.loweredName, this.Elements.Select(x => x.ToXml()));
}

/// <summary>
/// Represents a general description of this <see cref="Symbols.Symbol"/>.
/// </summary>
/// <param name="Elements"></param>
internal record class SummaryDocumentationSection(ImmutableArray<DocumentationElement> Elements) : DocumentationSection("summary", Elements)
{
    public override string ToMarkdown() =>
        string.Join("", this.Elements.Select(x => x.ToMarkdown()));
}

/// <summary>
/// Represents all parameters this <see cref="Symbols.Symbol"/> has.
/// </summary>
/// <param name="Parameters">The parameters references.</param>
internal record class ParametersDocumentationSection(ImmutableArray<ParameterDocumentationElement> Parameters) : DocumentationSection("Parameters", Parameters.Cast<DocumentationElement>().ToImmutableArray())
{
    public override string ToMarkdown() =>
        $"""
        # parameters
        {string.Join(Environment.NewLine, this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public override object ToXml() => this.Elements.Select(x => x.ToXml());
}

/// <summary>
/// Represents all type parameters this <see cref="Symbols.Symbol"/> has.
/// </summary>
/// <param name="TypeParameters">The type parameters references.</param>
internal record class TypeParametersDocumentationSection(ImmutableArray<TypeParameterDocumentationElement> TypeParameters) : DocumentationSection("TypeParameters", TypeParameters.Cast<DocumentationElement>().ToImmutableArray())
{
    public override string ToMarkdown() =>
        $"""
        # type parameters
        {string.Join(Environment.NewLine, this.Elements.Select(x => x.ToMarkdown()))}
        """;

    public override object ToXml() => this.Elements.Select(x => x.ToXml());
}

/// <summary>
/// Represents a section of code.
/// </summary>
/// <param name="Code">The code to display.</param>
internal record class CodeDocumentationSection(CodeDocumentationElement Code) : DocumentationSection("Code", ImmutableArray.Create<DocumentationElement>(Code))
{
    public override string ToMarkdown() => this.Code.ToMarkdown();
    public override object ToXml() => this.Code.ToXml();
}
