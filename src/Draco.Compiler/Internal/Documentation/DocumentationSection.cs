using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
internal sealed class DocumentationSection
{
    public string Name { get; }
    public SectionKind Kind { get; }
    public ImmutableArray<DocumentationElement> Elements { get; }

    private DocumentationSection(SectionKind kind, string name, ImmutableArray<DocumentationElement> elements)
    {
        this.Kind = kind;
        this.Name = name.ToLowerInvariant();
        this.Elements = elements;
    }

    public DocumentationSection(SectionKind kind, ImmutableArray<DocumentationElement> elements)
        : this(kind, GetSectionName(kind), elements)
    {
        // NOTE: GetSectionName throws on Other
    }

    public DocumentationSection(string name, ImmutableArray<DocumentationElement> elements)
        : this(GetSectionKind(name), name, elements)
    {
    }

    private static string GetSectionName(SectionKind kind) => kind switch
    {
        SectionKind.Summary => "summary",
        SectionKind.Parameters => "parameters",
        SectionKind.TypeParameters => "type parameters",
        SectionKind.Code => "code",
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
    };

    private static SectionKind GetSectionKind(string? name) => name switch
    {
        "summary" => SectionKind.Summary,
        "parameters" => SectionKind.Parameters,
        "type parameters" => SectionKind.TypeParameters,
        "code" => SectionKind.Code,
        _ => SectionKind.Other,
    };
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
