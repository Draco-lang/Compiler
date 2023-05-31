using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Draco.SourceGeneration.Dap.CsModel;
using Enum = Draco.SourceGeneration.Dap.CsModel.Enum;
using Type = Draco.SourceGeneration.Dap.CsModel.Type;

namespace Draco.SourceGeneration.Dap;

/// <summary>
/// Translates the JSON schema to a C# model.
/// </summary>
internal sealed class Translator
{
    private static readonly string[] basicStructures = new[]
    {
        "ProtocolMessage",
        "Request",
        "Response",
        "Event",
    };

    private readonly JsonDocument sourceModel;
    private readonly Model targetModel = new();
    private readonly Dictionary<string, Type> translatedTypes = new();

    public Translator(JsonDocument sourceModel)
    {
        this.sourceModel = sourceModel;
    }

    /// <summary>
    /// Adds a builtin type that does not need translation anymore.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The reflected type.</param>
    public void AddBuiltinType(string name, System.Type type) =>
        this.AddBuiltinType(name, type.FullName);

    /// <summary>
    /// Adds a builtin type that does not need translation anymore.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="fullName">The full name of the type.</param>
    public void AddBuiltinType(string name, string fullName) =>
        this.translatedTypes.Add(name, new CsModel.BuiltinType(fullName));

    /// <summary>
    /// Translated the source model to a C# model.
    /// </summary>
    /// <returns>The translated C# model.</returns>
    public Model Translate()
    {
        // Get all definitions in the schema
        var types = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject();

        foreach (var type in types)
        {
            var typeName = type.Name;
            var typeDesc = type.Value;

            // We skip basic structures
            if (basicStructures.Contains(typeName)) continue;

            // We skip requests, as all of their content is already among definitions as arguments
            if (typeName.EndsWith("Request")) continue;

            // Responses and Events have a "body" property
            if (typeName.EndsWith("Response"))
            {
                var innerTypeDesc = typeDesc
                    .GetProperty("allOf")
                    .EnumerateArray()
                    .Last();

                if (innerTypeDesc.TryGetProperty("body", out var body))
                {
                    // TODO
                    throw new NotImplementedException($"have body for {typeName}");
                }
                else
                {
                    // TODO
                    throw new NotImplementedException($"no body for {typeName}");
                }
            }

            // TODO
            throw new NotImplementedException($"not handled type definition {typeName}");
        }

        return this.targetModel;
    }

    private static bool TryGetEnum(
        JsonElement element,
        [MaybeNullWhen(false)] out IReadOnlyList<(string Name, string? Doc)> members)
    {
        var docs = null as IReadOnlyList<string?>;
        if (element.TryGetProperty("enumDescriptions", out var enumDesc))
        {
            docs = enumDesc
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();
        }
        if (element.TryGetProperty("enum", out var @enum))
        {
            docs ??= new string?[@enum.GetArrayLength()];
            members = @enum
                .EnumerateArray()
                .Zip(docs, (a, b) => (Name: a.GetString()!, Docs: b))
                .ToList();
            return true;
        }
        if (element.TryGetProperty("_enum", out var _enum))
        {
            docs ??= new string?[@enum.GetArrayLength()];
            members = _enum
                .EnumerateArray()
                .Zip(docs, (a, b) => (Name: a.GetString()!, Docs: b))
                .ToList();
            return true;
        }
        members = null;
        return false;
    }

    private static void ExtractDocumentation(JsonElement element, Declaration declaration)
    {
        if (element.TryGetProperty("title", out _)) { /* no-op*/ }
        if (element.TryGetProperty("description", out var doc)) declaration.Documentation = doc.GetString();
    }

    private static bool TryGetRef(JsonElement element, [MaybeNullWhen(false)] out string path)
    {
        if (element.TryGetProperty("$ref", out var value))
        {
            path = value.GetString()!;
            return true;
        }
        else
        {
            path = null;
            return false;
        }
    }

    private static bool IsNullString(JsonElement element) =>
           element.ValueKind == JsonValueKind.String
        && element.GetString() == "null";

    /// <summary>
    /// Translates a snake_case name to PascalCase.
    /// </summary>
    /// <param name="name">The name to translate.</param>
    /// <returns>The PascalCase version of <paramref name="name"/>.</returns>
    private static string ToPascalCase(string name)
    {
        var parts = name.Split('_');
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) result.Append('_');
            else result.Append(Capitalize(part));
        }
        return result.ToString();
    }

    /// <summary>
    /// Extracts a prefix from a name, which is the first capitalized word in it.
    /// </summary>
    /// <param name="name">The name to extract the prefix from.</param>
    /// <returns>The first capitalized word in <paramref name="name"/>.</returns>
    private static string ExtractNamePrefix(string name)
    {
        // Search for the second uppercase letter
        var endIndex = 1;
        for (; endIndex < name.Length; ++endIndex)
        {
            var ch = name[endIndex];
            if (ch == char.ToUpper(ch)) break;
        }
        // Cut it off
        return name[..endIndex];
    }

    /// <summary>
    /// Capitalizes a word.
    /// </summary>
    /// <param name="word">The word to capitalize.</param>
    /// <returns>The capitalized <paramref name="word"/>.</returns>
    [return: NotNullIfNotNull(nameof(word))]
    private static string? Capitalize(string? word) => word is null
        ? null
        : $"{char.ToUpper(word[0])}{word[1..]}";
}
