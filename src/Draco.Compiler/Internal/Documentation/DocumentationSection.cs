using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
internal sealed class DocumentationSection
{
    public string? Name { get; }
    public SectionKind Kind { get; }
    public ImmutableArray<DocumentationElement> Elements { get; }

    public DocumentationSection(SectionKind kind, ImmutableArray<DocumentationElement> elements)
    {
        this.Kind = kind;
        this.Elements = elements;
    }

    public DocumentationSection(string name, ImmutableArray<DocumentationElement> elements)
    {
        this.Name = name;
        this.Kind = SectionKind.Other;
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
