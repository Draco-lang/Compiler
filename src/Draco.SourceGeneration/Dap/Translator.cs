using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly JsonDocument sourceModel;
    private readonly Model targetModel = new();
    private readonly Dictionary<string, Type> builtinTypes = new();
    private readonly Dictionary<string, Declaration> translatedTypes = new();

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
        this.builtinTypes.Add(name, new CsModel.BuiltinType(fullName));

    /// <summary>
    /// Translated the source model to a C# model.
    /// </summary>
    /// <returns>The translated C# model.</returns>
    public Model Translate()
    {
        // Translate definitions
        var types = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject();
        foreach (var prop in types) this.TranslateDeclarationByPath($"#/definitions/{prop.Name}");

        return this.targetModel;
    }

    private Declaration TranslateDeclarationByPath(string path)
    {
        if (this.translatedTypes.TryGetValue(path, out var existing)) return existing;

        if (!path.StartsWith("#/definitions/")) throw new ArgumentException($"only definition references are supported, {path} is not one");
        var pathParts = path.Split('/');
        if (pathParts.Length != 3) throw new ArgumentException($"path {path} does not have exactly 3 nesting");

        var typeName = pathParts[^1];
        var typeToTranslate = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject()
            .FirstOrDefault(prop => prop.Name == typeName);
        if (typeToTranslate.Name is null) throw new KeyNotFoundException($"the type {typeName} could not be found for translation");

        return this.TranslateDeclaration(typeToTranslate.Name, typeToTranslate.Value, path: path);
    }

    private Declaration TranslateDeclaration(string name, JsonElement element, string? path)
    {
        // Class type with inheritance
        if (element.TryGetProperty("allOf", out var allOfElements))
        {
            // TODO
            throw new NotImplementedException($"not implemented declaration {name}");
        }

        // Enum
        if (TryGetEnum(element, out var enumMembers, out var enumDocs) && enumMembers.Count > 1)
        {
            var result = this.TranslateEnumDeclaration(name, enumMembers, enumDocs, path: path);
            ExtractDocumentation(element, result);
            return result;
        }

        // Tagged type
        if (element.TryGetProperty("type", out var typeTag))
        {
            var typeName = typeTag.GetString();
            // TODO
            throw new NotImplementedException($"not implemented tag {typeName} for {name}");
        }

        // Unknown
        throw new NotImplementedException($"can not recognize type {name}");
    }

    private Enum TranslateEnumDeclaration(
        string name,
        IReadOnlyList<string> members,
        IReadOnlyList<string>? docs,
        string? path)
    {
        var result = new Enum() { Name = name };
        if (path is not null) this.translatedTypes.Add(path, result);

        // Add members
        for (var i = 0; i < members.Count; ++i)
        {
            var member = new EnumMember()
            {
                Name = ToPascalCase(members[i]),
                Documentation = docs?[i],
                Value = members[i],
            };
            result.Members.Add(member);
        }

        return result;
    }

    private static bool TryGetEnum(
        JsonElement element,
        [MaybeNullWhen(false)] out IReadOnlyList<string> members,
        out IReadOnlyList<string>? docs)
    {
        docs = null;
        if (element.TryGetProperty("enumDescriptions", out var enumDesc))
        {
            docs = enumDesc
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
        }
        if (element.TryGetProperty("enum", out var @enum))
        {
            members = @enum
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
            return true;
        }
        if (element.TryGetProperty("_enum", out var _enum))
        {
            members = _enum
                .EnumerateArray()
                .Select(e => e.GetString()!)
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
