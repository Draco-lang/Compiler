using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
internal sealed class DocumentationSection
{
    public string? Name { get; }
    public ImmutableArray<DocumentationElement> Elements { get; }

    public SectionKind Kind => this.kind ?? SectionKind.Other;
    private SectionKind? kind;

    public DocumentationSection(SectionKind sectionKind, ImmutableArray<DocumentationElement> elements)
    {
        this.kind = sectionKind;
        this.Elements = elements;
    }

    public DocumentationSection(string name, ImmutableArray<DocumentationElement> elements)
    {
        this.Name = name;
        this.Elements = elements;
    }
}

// Note: The values of the sections are used for ordering from smallest to highest
internal enum SectionKind
{
    Summary = 1,
    Parameters = 2,
    TypeParameters = 3,
    Code = 4,
    Other = 5,
}
