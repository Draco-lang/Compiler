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
        foreach (var prop in types)
        {
            var type = this.TranslateDeclarationByPath($"#/definitions/{prop.Name}");
            if (type is DeclarationType declType) this.targetModel.Declarations.Add(declType.Declaration);
        }

        return this.targetModel;
    }

    private Type TranslateDeclarationByPath(string path)
    {
        if (this.translatedTypes.TryGetValue(path, out var existing)) return new DeclarationType(existing);

        if (!path.StartsWith("#/definitions/")) throw new ArgumentException($"only definition references are supported, {path} is not one");
        var pathParts = path.Split('/');
        if (pathParts.Length != 3) throw new ArgumentException($"path {path} does not have exactly 3 nesting");

        var typeName = pathParts[^1];
        var typeToTranslate = this.sourceModel.RootElement
            .GetProperty("definitions")
            .EnumerateObject()
            .FirstOrDefault(prop => prop.Name == typeName);
        if (typeToTranslate.Name is null) throw new KeyNotFoundException($"the type {typeName} could not be found for translation");

        return this.TranslateType(typeToTranslate.Value, nameHint: typeToTranslate.Name, parent: null, path: path);
    }

    private Type TranslateType(JsonElement element, string? nameHint, Class? parent, string? path)
    {
        // Builtin type
        if (element.ValueKind == JsonValueKind.String) return this.builtinTypes[element.GetString()!];

        // Reference type
        if (TryGetRef(element, out var typePath)) return this.TranslateDeclarationByPath(typePath);

        // Class type with inheritance
        if (element.TryGetProperty("allOf", out var allOfElements))
        {
            // Assume first element is the base type by ref
            // Second is the derived class
            var parts = allOfElements.EnumerateArray().ToList();
            // Extract base path
            if (!TryGetRef(parts[0], out var basePath)) throw new InvalidOperationException($"allOf type {nameHint} did not follow the assumed pattern");
            // Build derived type
            var derivedType = this.TranslateType(parts[1], nameHint: nameHint, parent: parent, path: path);
            // Connect up
            var derivedClass = (Class)((DeclarationType)derivedType).Declaration;
            var baseType = this.TranslateDeclarationByPath(basePath);
            var baseClass = (Class)((DeclarationType)baseType).Declaration;
            derivedClass.Base = baseClass;
            this.ProcessInheritedProperties(derivedClass);
            return derivedType;
        }

        // DU in yet another form
        if (element.TryGetProperty("oneOf", out var oneOfElements))
        {
            var elements = oneOfElements.EnumerateArray().ToList();
            return this.TranslateDiscriminatedUnionType(elements, nameHint: nameHint, parent: parent);
        }

        // Enum
        if (TryGetEnum(element, out var enumMembers, out var enumDocs) && enumMembers.Count > 1)
        {
            if (nameHint is null) throw new ArgumentNullException(nameof(nameHint));
            var name = parent is null
                ? nameHint
                : $"{ExtractNamePrefix(parent.Name)}{nameHint}";
            var result = this.TranslateEnumDeclaration(name, enumMembers, enumDocs, parent: parent, path: path);
            ExtractDocumentation(element, result);
            return new DeclarationType(result);
        }

        // Tagged type
        if (element.TryGetProperty("type", out var typeTag))
        {
            if (typeTag.ValueKind == JsonValueKind.String)
            {
                var typeName = typeTag.GetString()!;

                // Builtin type
                if (this.builtinTypes.TryGetValue(typeName, out var type)) return type;

                // Class type
                if (typeName == "object")
                {
                    if (nameHint is null) throw new ArgumentNullException(nameof(nameHint));
                    var name = parent is null
                        ? nameHint
                        : $"{ExtractNamePrefix(parent.Name)}{nameHint}";
                    var result = this.TranslateClassDeclaration(name, element, parent: parent, path: path);
                    ExtractDocumentation(element, result);
                    return new DeclarationType(result);
                }

                // Array type
                if (typeName == "array")
                {
                    var itemElement = element.GetProperty("items");
                    var elementType = this.TranslateType(itemElement, nameHint: nameHint, parent: parent, path: null);
                    return new ArrayType(elementType);
                }

                // TODO
                return new CsModel.BuiltinType($"Unknown<{typeTag.GetRawText()}>");
            }
            if (typeTag.ValueKind == JsonValueKind.Array)
            {
                var elements = typeTag.EnumerateArray().ToList();
                return this.TranslateDiscriminatedUnionType(elements, nameHint: nameHint, parent: parent);
            }
            else
            {
                // TODO
                return new CsModel.BuiltinType($"Unknown<{typeTag.GetRawText()}>");
            }
        }

        // TODO
        return new CsModel.BuiltinType($"Unknown<{element.GetRawText()}>");
        // Unknown
        throw new NotImplementedException($"can not recognize type {element.GetRawText()}");
    }

    private Type TranslateDiscriminatedUnionType(
        IReadOnlyList<JsonElement> alternatives,
        string? nameHint,
        Class? parent)
    {
        // Assume any
        if (alternatives.Count >= 7) return this.builtinTypes["any"];

        // If there's only one left, not a DU
        if (alternatives.Count == 1) return this.TranslateType(alternatives[0], nameHint: nameHint, parent: parent, path: null);

        // Check, if there's a null among them
        if (alternatives.Any(IsNullString))
        {
            // Translate the rest and make it nullable
            var nonNullAlternatives = alternatives
                .Where(a => !IsNullString(a))
                .ToList();
            var subtype = this.TranslateDiscriminatedUnionType(nonNullAlternatives, nameHint: nameHint, parent: parent);
            return new NullableType(subtype);
        }

        // Regular DU
        var translatedAlternatives = alternatives
            .Select(a => this.TranslateType(a, nameHint: nameHint, parent: parent, path: null))
            .ToImmutableArray();
        return new DiscriminatedUnionType(translatedAlternatives);
    }

    private Enum TranslateEnumDeclaration(
        string name,
        IReadOnlyList<string> members,
        IReadOnlyList<string>? docs,
        Class? parent,
        string? path)
    {
        var result = new Enum() { Name = name };
        if (path is not null) this.translatedTypes.Add(path, result);

        if (parent is not null) parent.NestedDeclarations.Add(result);

        // Add members
        for (var i = 0; i < members.Count; ++i)
        {
            var member = new EnumMember()
            {
                DeclaringEnum = result,
                Name = ToPascalCase(members[i].Replace(' ', '_')),
                Documentation = docs?[i],
                Value = members[i],
            };
            result.Members.Add(member);
        }

        return result;
    }

    private Class TranslateClassDeclaration(string name, JsonElement element, Class? parent, string? path)
    {
        var result = new Class()
        {
            Name = name,
            Parent = parent,
        };
        if (path is not null) this.translatedTypes.Add(path, result);

        if (parent is not null)
        {
            parent.NestedDeclarations.Add(result);
            result.Parent = parent;
        }

        if (element.TryGetProperty("properties", out var props))
        {
            var requiredPropNames = element.TryGetProperty("required", out var reqProps)
                ? reqProps.EnumerateArray().Select(e => e.GetString()!).ToHashSet()
                : new HashSet<string>();

            foreach (var prop in props.EnumerateObject())
            {
                var translatedProp = this.TranslatePropertyDeclaration(prop.Name, prop.Value, parent: result);
                if (translatedProp is null) continue;

                ExtractDocumentation(prop.Value, translatedProp);
                result.Properties.Add(translatedProp);

                if (!requiredPropNames.Contains(translatedProp.SerializedName))
                {
                    // Optional prop
                    translatedProp.OmitIfNull = true;
                    if (translatedProp.Type is not NullableType) translatedProp.Type = new NullableType(translatedProp.Type);
                }
                else
                {
                    // Mark with required
                    translatedProp.IsRequired = true;
                }
            }
        }

        return result;
    }

    private void ProcessInheritedProperties(Class @class)
    {
        Debug.Assert(@class.Base is not null);

        foreach (var prop in @class.Properties)
        {
            var baseProp = @class.Base!.Properties.FirstOrDefault(p => p.SerializedName == prop.SerializedName);
            if (baseProp is null) continue;

            if (baseProp.Type != prop.Type && prop.Value is null)
            {
                // This is likely a specialized property, for simplicity we remove it from base
                @class.Base.Properties.Remove(baseProp);
                continue;
            }

            // There is such a property in base
            prop.Overrides = baseProp;
            prop.Type = baseProp.Type;
            prop.Name = baseProp.Name;
            baseProp.IsAbstract = true;
            @class.Base.IsAbstract = true;

            // If the derived property has a value that is a string, but the type is now an enum,
            // it means that the property specializes an enum member, replace
            if (prop.Value is string && baseProp.Type is DeclarationType { Declaration: Enum baseEnum })
            {
                var baseEnumMember = baseEnum.Members.First(m => prop.Value.Equals(m.Value));
                prop.Value = baseEnumMember;
            }
        }
    }

    private Property? TranslatePropertyDeclaration(string name, JsonElement element, Class parent)
    {
        var result = new Property()
        {
            Name = ToPascalCase(name),
            SerializedName = name,
        };

        // Adjust name to avoid name collisions
        if (parent.Name == result.Name) result.Name = $"{result.Name}_";

        // Translate the type
        result.Type = this.TranslateType(element, nameHint: result.Name, parent: parent, path: null);

        // Check if it's a singleton value
        if (element.TryGetProperty("enum", out var alternatives) && alternatives.GetArrayLength() == 1)
        {
            var value = alternatives.EnumerateArray().First().GetString()!;
            result.Value = value;
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

    private static JsonElement ExtractContent(string name, JsonElement element)
    {
        JsonElement Extract(string propName)
        {
            var root = element;
            if (root.TryGetProperty("allOf", out var allOf)) root = allOf.EnumerateArray().Last();
            if (!root.TryGetProperty("properties", out var props)) return default;
            props.TryGetProperty(propName, out var prop);
            return prop;
        }

        if (name.EndsWith("Request")) return Extract("arguments");
        if (name.EndsWith("Response") || name.EndsWith("Event")) return Extract("body");
        return element;
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
