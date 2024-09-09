using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.Dap;

internal static class Template
{
    public static string Generate(CsModel.Model model) => FormatCSharp($$"""
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Draco.Dap.Serialization;

namespace Draco.Dap.Model;

#nullable enable
#pragma warning disable CS9042

{{ForEach(model.Declarations, Declaration)}}

#pragma warning restore CS9042
#nullable restore
""");

    private static string Declaration(CsModel.Declaration declaration) => $$"""
    {{Summary(declaration)}}
    {{declaration switch
    {
        CsModel.Class @class => Class(@class),
        CsModel.Enum @enum => Enum(@enum),
        _ => string.Empty,
    }}}
    """;

    private static string Class(CsModel.Class @class) => $$"""
    public class {{@class.Name}} {{Base(@class)}}
    {
        {{ForEach(@class.NestedDeclarations, Declaration)}}
        {{ForEach(@class.Properties, Property)}}
    }
    """;

    private static string Enum(CsModel.Enum @enum) => $$"""
    {{When(@enum.IsStringEnum, "[JsonConverter(typeof(EnumValueConverter))]")}}
    public enum {{@enum.Name}}
    {
        {{ForEach(@enum.Members, ", ", EnumMember)}}
    }
    """;

    private static string Property(CsModel.Property property) => $$"""
    {{Summary(property)}}
    [JsonPropertyName("{{Unescape(property.SerializedName)}}")]
    [JsonIgnore(Condition = JsonIgnoreCondition.{{When(property.OmitIfNull, "WhenWritingDefault", "Never")}})]
    {{When(property.IsRequired, "[JsonRequired]")}}
    public {{When(property.IsRequired, "required")}} {{Type(property.Type)}} {{property.Name}}
    {{When(property.Value is not null,
        whenTrue: $"""=> {property.Value switch
        {
            string s => $"\"{Unescape(s)}\"",
            null => "null",
            var v => v.ToString(),
        }}""",
        whenFalse: "{ get; set; }")}}
    """;

    private static string EnumMember(CsModel.EnumMember member) => $$"""
    {{Summary(member)}}
    {{member.Value switch
    {
        int i => $"{member.Name} = {i}",
        string s => $"""
            [EnumMember(Value = "{Unescape(s)}")]
            {member.Name}
            """,
        _ => member.Name,
    }}}
    """;

    private static string Type(CsModel.Type type) => type switch
    {
        CsModel.BuiltinType b => b.FullName,
        CsModel.NullableType n => $"{Type(n.Type)}?",
        CsModel.ArrayType a => $"IList<{Type(a.ElementType)}>",
        CsModel.DiscriminatedUnionType du => $"OneOf<{ForEach(du.Alternatives, ", ", Type)}>",
        CsModel.DictionaryType d => $"IDictionary<{Type(d.KeyType)}, {Type(d.ValueType)}>",
        CsModel.DeclarationType decl => decl.Declaration.Name,
        _ => "/* unknown type */",
    };

    private static string Base(CsModel.Class @class) =>
        NotNull(@class.Base, b => $": {b.Name}");

    private static string Summary(CsModel.Declaration declaration) => NotNull(declaration.Documentation, doc => $"""
    /// <summary>
    {ForEachLine(doc, line => $"/// {line}")}
    /// </summary>
    """);
}
