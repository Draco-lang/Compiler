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
    public SectionKind Kind => this.kind ?? SectionKind.Other;
    private SectionKind? kind;

    public DocumentationSection(SectionKind sectionKind, ImmutableArray<DocumentationElement> Elements)
        : this(sectionKind switch
        {
            SectionKind.Summary => "summary",
            SectionKind.Parameters => "parameters",
            SectionKind.TypeParameters => "type parameters",
            SectionKind.Code => "code",
            _ => "unknown section",
        }, Elements)
    {
        this.kind = sectionKind;
    }
}

internal enum SectionKind
{
    Summary = 1,
    Parameters = 2,
    TypeParameters = 3,
    Code = 4,
    Other = 5,
}
