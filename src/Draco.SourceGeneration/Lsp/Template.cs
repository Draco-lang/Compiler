using System.Collections.Generic;
using System.Linq;
using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.Lsp;

internal static class Template
{
    public static string Generate(CsModel.Model model) => FormatCSharp($$"""
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Draco.Lsp.Attributes;
using Draco.Lsp.Serialization;

namespace Draco.Lsp.Model;

#nullable enable
#pragma warning disable CS9042

{{ForEach(model.Declarations, Declaration)}}

#pragma warning restore CS9042
#nullable restore
""");

    private static string Declaration(CsModel.Declaration declaration) => $$"""
    {{Summary(declaration)}}
    {{Obsolete(declaration)}}
    {{declaration switch
    {
        CsModel.Class @class => Class(@class),
        CsModel.Interface @interface => Interface(@interface),
        CsModel.Enum @enum => Enum(@enum),
        _ => string.Empty,
    }}}
    """;

    private static string Class(CsModel.Class @class) => $$"""
    public sealed class {{@class.Name}} {{Bases(@class.Interfaces)}}
    {
        {{ForEach(@class.NestedDeclarations, Declaration)}}
        {{ForEach(@class.Properties, p => Property(p, true))}}
    }
    """;

    private static string Interface(CsModel.Interface @interface) => $$"""
    public interface {{@interface.Name}} {{Bases(@interface.Interfaces)}}
    {
        {{ForEach(@interface.Properties, p => Property(p, false))}}
    }
    """;

    private static string Enum(CsModel.Enum @enum) => $$"""
    {{When(@enum.IsStringEnum, "[JsonConverter(typeof(EnumValueConverter))]")}}
    public enum {{@enum.Name}}
    {
        {{ForEach(@enum.Members, ", ", EnumMember)}}
    }
    """;

    private static string Property(CsModel.Property property, bool settable) => $$"""
    {{Summary(property)}}
    {{Obsolete(property)}}
    [JsonPropertyName("{{Unescape(property.SerializedName)}}")]
    [JsonIgnore(Condition = JsonIgnoreCondition.{{When(property.OmitIfNull, "WhenWritingDefault", "Never")}})]
    {{When(property.IsRequired(settable), "[JsonRequired]")}}
    public {{When(property.IsRequired(settable), "required")}} {{Type(property.Type)}} {{property.Name}}
    {{When(property.Value is not null,
        whenTrue: $$"""
        => {{property.Value switch
        {
            string s => $"\"{Unescape(s)}\"",
            null => "null",
            var v => v.ToString(),
        }}};
        """,
        whenFalse: $$"""
        { get; {{When(settable, "set;")}} }
        """)}}
    """;

    private static string EnumMember(CsModel.EnumMember member) => $$"""
    {{Summary(member)}}
    {{Obsolete(member)}}
    {{member.Value switch
    {
        int i => $"{member.Name} = {i}",
        string s => $"""
            [EnumMember(Value = "{Unescape(s)}")]
            {member.Name}
            """,
        _ => $"{member.Name}",
    }}}
    """;

    private static string Type(CsModel.Type type) => type switch
    {
        CsModel.BuiltinType builtin => builtin.FullName,
        CsModel.NullableType nullable => $"{Type(nullable.Type)}?",
        CsModel.ArrayType array => $"IList<{Type(array.ElementType)}>",
        CsModel.DiscriminatedUnionType du => $"OneOf<{ForEach(du.Alternatives, ", ", Type)}>",
        CsModel.DictionaryType dict => $"IDictionary<{Type(dict.KeyType)}, {Type(dict.ValueType)}>",
        CsModel.TupleType tuple => $"({ForEach(tuple.Elements, ", ", Type)})",
        CsModel.DeclarationType decl => decl.Declaration switch
        {
            CsModel.Class @class => ClassType(@class),
            _ => decl.Declaration.Name,
        },
        _ => "/* unknown type */",
    };

    private static string ClassType(CsModel.Class @class) => @class.Parent is null
        ? @class.Name
        : $"{ClassType(@class.Parent)}.{@class.Name}";

    private static string Bases(IEnumerable<CsModel.Interface> interfaces) =>
        When(interfaces.Any(), $" : {ForEach(interfaces, ", ", i => i.Name)}");

    private static string Summary(CsModel.Declaration declaration) => NotNull(declaration.Documentation, doc => $"""
    /// <summary>
    {ForEachLine(doc, line => $"/// {line}")}
    /// </summary>
    """);

    private static string Obsolete(CsModel.Declaration declaration) => NotNull(declaration.Deprecated, depr => $"""
    [Obsolete("{depr}")]
    """);
}
