using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
/// <param name="Name">The name of the section</param>
/// <param name="Elements">The <see cref="DocumentationElement"/>s this section contains.</param>
internal sealed record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements)
{
    private static Dictionary<SectionKind, string> wellKnownSections = new()
    {
        { SectionKind.Summary, "summary" },
        { SectionKind.Parameters, "parameters" },
        { SectionKind.TypeParameters, "type parameters" },
        { SectionKind.Code, "code" },
    };

    public SectionKind Kind => this.kind ?? SectionKind.Other;
    private SectionKind? kind;

    public DocumentationSection(SectionKind sectionKind, ImmutableArray<DocumentationElement> Elements)
        : this(wellKnownSections[sectionKind], Elements)
    {
        this.kind = sectionKind;
    }
}

internal enum SectionKind
{
    Other,
    Summary,
    Parameters,
    TypeParameters,
    Code,
}
